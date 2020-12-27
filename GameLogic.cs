﻿using System;
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
            (var pos, int tz_boost) = MovePlayerCore(player, input_direction);
            if (pos is Vector2 new_position)
            {
                player.position = new_position;
                if (tz_boost != 0)
                {
                    // Temporarily boost the Z level. Client side this only
                    // controls the Z order which is handy.
                    var zl = player.z_level;
                    player.z_level = tz_boost;
                    ServerSend.PlayerPosition(player);
                    player.z_level = zl;
                }
                else
                {
                    ServerSend.PlayerPosition(player);
                }

                // Client is authoritative for rotation: update not sent back to self.
                ServerSend.PlayerRotation(player);
            }
        }

        internal (Vector2?, int tz_boost) MovePlayerCore(Player player, Vector2 input_direction)
        {
            if (player.transit_state == Player.TransitState.Frozen)
            {
                // We should not see this because AddPlayer() sets
                // the player to Ground transitstate.
                return (null, 0);
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
                return (null, 0);
            }

            // The movement is a state machine. Note that for vertical movement
            // to happen we need a careful physical arrangement of objects in the
            // map. In particular the stairs are actually comprised of 4 parts:
            //
            // threshold  (at level N)
            // lower stair (at level N)
            // upper stair (at level M)
            // upper threshold (at level M)
            //
            // So it works like this. Stairs are not passable directly, only
            // via thresholds which can exit into the stairs (and any other thing),
            // once in stair mode it can only be exited to other stair no matter
            // which level and to a threshold which can be the previously traversed
            // or the one at the other end.

            var zones = map_handler_.GetZonesForPoint(point, player.z_level);
 
            switch (player.transit_state)
            {
                case Player.TransitState.Ground:
                    if (zones == ZoneBits.None)
                    {
                        return (newPosition, 0);
                    }
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        player.transit_state = Player.TransitState.Threshold;
                        return (newPosition, 0);
                    }
                    if (zones.Contains(ZoneBits.Keep))
                    {
                        // TODO: remove this case. Keeps are meant to work
                        // the other way.
                        return (newPosition, 0);
                    }

                    // All other zones (Boulders, Water deep) are not passable.
                    break;
                case Player.TransitState.Threshold:
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        return (newPosition, 0);
                    }
                    if (zones.Contains(ZoneBits.Stairs))
                    {
                        player.transit_state = Player.TransitState.Stairs;
                        player.stair_level = map_handler_.GetStairLevelForPoint(point);
                        // The player is on the stairs, temporarily boost the z order.
                        return (newPosition, 3);
                    }
                    // everything else means the player exited
                    player.transit_state = Player.TransitState.Ground;
                    return (newPosition, 0);

                case Player.TransitState.Stairs:
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        player.transit_state = Player.TransitState.Threshold;
                        return (newPosition, 0);
                    }

                    var new_stair_level = map_handler_.GetStairLevelForPoint(point);
                    if (new_stair_level < 0)
                    {
                        break;
                    }

                    if (new_stair_level != player.stair_level)
                    {
                        Console.WriteLine($"+ [{player.user_name}] level {new_stair_level}");
                    }

                    player.z_level = new_stair_level;
                    return (newPosition, 0);
                default:
                    throw new Exception("unexpected state");
            }

            // No movement allowed.
            return (null, 0);
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
