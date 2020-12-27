using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Drawing;

namespace TFIServer
{
    [Flags]
    enum GameLogicOptions
    {
        None = 0,
        Heartbeat = 1
    }
    static class GLOExtensions
    {
        public static bool HasHeartbeat(this GameLogicOptions options)
        {
            return (options & GameLogicOptions.Heartbeat) == GameLogicOptions.Heartbeat;
        }
    }

    class GameLogic
    {
        private readonly Dictionary<int, Player> players_;
        private long last_ticks_ = 0;
        private GameLogicOptions options_ = 0;
        private MapHandler map_handler_;
        private RectangleF map_extents_;

        // The client must have the same ppu value.
        private readonly int pixels_per_unit_ = 32;

        public GameLogic(string map)
        {
            players_ = new Dictionary<int, Player>();
            map_handler_ = new MapHandler(pixels_per_unit_);
            map_handler_.LoadMapJSON(map);

            map_extents_ = new RectangleF(
                0, 0, map_handler_.Column_count, map_handler_.Row_count);
        }

        public void AddPlayer(int id, string player_name)
        {
            // First lets send the map.
            map_handler_.SendMap(id);

            var new_player = new Player(id, player_name, GetSpawnPoint(), 0);
            new_player.transit_state = Player.TransitState.Ground;
            new_player.threshold_level = 0;

            // Tell new player about all other existing players
            foreach (Player player in players_.Values)
            {
                ServerSend.SpawnPlayer(new_player.id, player);
            }

            players_[id] = new_player;

            // Tell all players (including self) about new player,
            foreach (Player player in players_.Values)
            {
                ServerSend.SpawnPlayer(player.id, new_player);
            }

            Console.WriteLine($"+ [{player_name}] accepted as player {id} @ {new_player.position}.");
        }
        public void UpdateFixed(long ticks)
        {
            var actions = ThreadManager.ExternalUpdate(this);
            if (actions == 0)
            {
                if (options_.HasHeartbeat())
                {
                    var ms_delta = (ticks - last_ticks_) / TimeSpan.TicksPerMillisecond;
                    if (ms_delta > 5000)
                    {
                        var seconds = ticks / TimeSpan.TicksPerSecond;
                        Console.WriteLine($"{seconds} ena {ServerHandle.packets_recv_tcp} {ServerHandle.packets_recv_udp}");
                        last_ticks_ = ticks;
                    }
                }
                return;
            }

            foreach (Player player in players_.Values)
            {
                player.Update(this);
            }

            last_ticks_ = ticks;
        }

        internal void PlayerQuit(int from_client)
        {
            if (!players_.TryGetValue(from_client, out var player))
            {
                return;
            }

            _ = players_.Remove(from_client);
            ServerSend.PlayerQuit(player, 0);
        }

        internal void PlayerInput(int from_client, bool[] inputs, Quaternion rotation)
        {
            players_[from_client].SetInput(inputs, rotation);
        }

        internal void MovePlayer(Player player, Vector2 input_direction)
        {
            var pos = MovePlayerCore(player, input_direction);
            if (pos is Vector2 new_position)
            {
                player.position = new_position;
                ServerSend.PlayerPosition(player);
                // Client is authoritative for rotation: update not sent back to self.
                ServerSend.PlayerRotation(player);
            }
        }

        internal Vector2? MovePlayerCore(Player player, Vector2 input_direction)
        {
            if (player.transit_state == Player.TransitState.Frozen)
            {
                // We should not see this because AddPlayer() sets
                // the player to Ground transitstate.
                return null;
            }
            
            // For 3D, Z is forward (towards screen) and +Y is up.
            // Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            // Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            Vector2 forward = Vector2.UnitY;
            Vector2 right = -Vector2.UnitX;

            var move_direction = (right * input_direction.X) + (forward * input_direction.Y);
            var newPosition = player.position + (move_direction * player.move_speed);

            var point = new PointF(newPosition.X, newPosition.Y);

            if (!map_extents_.Contains(point)) {
                // Keep the players in the map.
                return null;
            }

            var zones = map_handler_.GetZonesForPoint(point, player.z_level);
            if (zones == ZoneBits.None && player.transit_state == Player.TransitState.Ground)
            {
                // Short circuit all other calculations.
                player.threshold_level = 0;
                return newPosition;
            }

            // Stair handling.
            // This is an approach that even though I just cooked it
            // needs a serious re-think. The premise here is that all
            // map objects are still a single layer so traversing stairs
            // is a state machine with the map objects as follows
            // - Threshold zone A on the bottom
            // - stairs zone
            // - Threshold zone B on the top
            // These 3 zones are overlapping:
            //
            //   [              (  ]        [  )          ]
            //     threshold A       stairs   threshold B
            //
            // The idea is that the
            // player traverses them in either order which when done
            // it changes the Z axis by +1 or -1 which client side
            // should change the sort order. The |threshold_level|
            // tracks the current threshold so the value should go
            // from 0 to A to B or from 0 to B to A.

            int new_threshold_level = 0;  // <-- fix zones.GetThreshold();
            if (new_threshold_level != 0)
            {
                // Thresholds are always traversable. Even overlapping with other zones.
                if (player.transit_state == Player.TransitState.Ground)
                {
                    player.threshold_level = new_threshold_level;
                    return newPosition;
                }

                // Player is in stairs mode.
                var diff = new_threshold_level - player.threshold_level;
                if (diff == 0)
                {
                    // Same threshold.
                    return newPosition;
                }
                else if (diff > 0)
                {
                    // Reached an upper level.
                    player.z_level += 1;
                } 
                else if (diff < 0)
                {
                    // Reached a lower level.
                    player.z_level -= 1;
                }

                Console.WriteLine($"+ [{player.user_name}] at level {player.z_level}");

                player.transit_state = Player.TransitState.Ground;
                player.threshold_level = new_threshold_level;
                return newPosition;
            } else

            // Player not in a threshold.

            if (player.threshold_level != 0 && zones.Contains(ZoneBits.Stairs))
            {
                // Player was in a threshold and now has entered the stairs.
                player.transit_state = Player.TransitState.Stairs;
                return newPosition;
            }

            if (player.transit_state == Player.TransitState.Stairs)
            {
                // Player in stairs mode needs to keep confined to the stairs
                // until it exits it via a threshold.
                if (!zones.Contains(ZoneBits.Stairs))
                {
                    return null;
                }
                // Still on stairs, which are always traversable. TODO: Here we
                // should be applying some "Y" offset.
                return newPosition;
            }

            // Player in ground mode.
            if (zones.Contains(ZoneBits.WaterDeep) ||
                zones.Contains(ZoneBits.Boulders) ||
                zones.Contains(ZoneBits.Stairs))
            {
                // Can't walk on water, stairs or across boulders.
                return null;
            }

            return newPosition;
        }

        internal void Connect(int id)
        {
            ServerSend.Welcome(id, pixels_per_unit_, map_handler_);
        }

        internal void Disconnect(int id)
        {
            PlayerQuit(id);
            Console.WriteLine($"+ player {id} disconnected.");
        }

        internal void ToggleHeartbeatPrint()
        {
            if (options_.HasHeartbeat())
            {
                options_ &= ~GameLogicOptions.Heartbeat;
            }
            else
            {
                options_ |= GameLogicOptions.Heartbeat;
            }
        }

        internal Vector2 GetSpawnPoint()
        {
            foreach (var spawn in map_handler_.GetPlayerSpawns())
            {
                foreach (var player in players_.Values)
                {
                    if (spawn.Contains(player.position.X, player.position.Y))
                    {
                        goto found;
                    }
                }
                // Nobody in this spawn point, use it.
                return GetMidRectVect(spawn);

            found:;
            }
            // No spawn point free! TODO: do something better.
            return new Vector2(20, 20);
        }

        internal Vector2 GetMidRectVect(RectangleF r)
        {
            return new Vector2((r.X + r.Width / 2), (r.Y + r.Height / 2));
        }

        internal void DumpPlayers() 
        {
            if (players_.Count == 0)
            {
                Console.WriteLine("no players");
                return;
            }

            StringBuilder sb = new StringBuilder(120);
            foreach (var p in players_.Values)
            {
                sb.AppendLine($"   player {p.id} : {p.user_name} @ {p.position}");
            }

            Console.Write(sb.ToString());
        }
    }
}
