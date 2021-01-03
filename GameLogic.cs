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
        private readonly MapHandler map_handler_;
        private Rectangle map_extents_;

        public GameLogic(string map)
        {
            players_ = new Dictionary<int, Player>();
            map_handler_ = new MapHandler();
            map_handler_.LoadMapJSON(map);

            map_extents_ = new Rectangle(Point.Empty, map_handler_.Pixels_size);
        }

        public void AddPlayer(int id, string player_name)
        {
            // First, lets send the map.
            map_handler_.SendMap(id);

            var new_player = new Player(id, player_name, GetSpawnPoint(), 0);

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

            Console.WriteLine($"+ [{player_name}] accepted as player {id} @ {new_player.Position}.");
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
                Console.WriteLine($"invalid quit from {from_client} client id");
                return;
            }

            _ = players_.Remove(from_client);
            ServerSend.PlayerQuit(player, 0);
        }

        internal void PlayerInput(int from_client, bool[] inputs)
        {
            if (!players_.TryGetValue(from_client, out var player))
            {
                Console.WriteLine($"invalid input from {from_client} client id");
                return;
            }

            player.SetInput(inputs);
        }

        // Given a proposed new position and the current player state this function
        // computes the new state (which might include the new position) and a temporary
        // z-level boost.
        internal (PlayerState?, int tz_boost) MovePlayer(in PlayerState state, Point new_position)
        {
            if (state.transit_state == TransitState.Frozen)
            {
                // We should not see this because AddPlayer() sets
                // the player to Ground transitstate.
                return (null, 0);
            }
            
            if (!map_extents_.Contains(new_position)) {
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

            var zones = map_handler_.GetZonesForPoint(new_position, state.z_level);
 
            switch (state.transit_state)
            {
                case TransitState.Ground:
                    if (zones == ZoneBits.None)
                    {
                        return (new PlayerState(state, new_position), 0);
                    }
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        return (new PlayerState(state, TransitState.Threshold, new_position), 0);
                    }
                    // All other zones (Boulders, Water deep) are not passable.
                    break;
                case TransitState.Threshold:
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        return (new PlayerState(state, new_position), 0);
                    }
                    if (zones.Contains(ZoneBits.Stairs))
                    {
                        // The player is on the stairs, temporarily boost the z order.
                        return (new PlayerState(state, TransitState.Stairs, new_position), 3);
                    }
                    if (zones.Contains(ZoneBits.ClosedArea))
                    {
                        return (new PlayerState(state, TransitState.ClosedArea, new_position), 0);
                    }
                    // everything else means the player exited
                    return (new PlayerState(state, TransitState.Ground, new_position), 0);

                case TransitState.Stairs:
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        return (new PlayerState(state, TransitState.Threshold, new_position), 0);
                    }

                    var new_stair_level = map_handler_.GetStairLevelForPoint(new_position);
                    if (new_stair_level < 0)
                    {
                        // Can't exit stairs without a threshold.
                        return (null, 3);
                    }

                    return (new PlayerState(state, new_stair_level, new_position), 3);

                case TransitState.ClosedArea:
                    if (zones.Contains(ZoneBits.Threshold))
                    {
                        return (new PlayerState(state, TransitState.Threshold, new_position), 0);
                    }
                    if (zones.Contains(ZoneBits.ClosedArea))
                    {
                        return (new PlayerState(state, new_position), 0);
                    }
                     break;
                default:
                    throw new Exception("unexpected state");
            }

            // No movement allowed.
            return (null, 0);
        }

        internal void Connect(int id)
        {
            ServerSend.Welcome(id, map_handler_);
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

        internal Point GetSpawnPoint()
        {
            foreach (var spawn in map_handler_.GetPlayerSpawns())
            {
                foreach (var player in players_.Values)
                {
                    if (spawn.Contains(player.Position.X, player.Position.Y))
                    {
                        goto found;
                    }
                }
                // Nobody in this spawn point, use it.
                return GetMidRectVect(spawn);

            found:;
            }
            // No spawn point free! TODO: do something better.
            return new Point(200, 200);
        }

        internal Point GetMidRectVect(Rectangle r)
        {
            return new Point((r.X + r.Width / 2), (r.Y + r.Height / 2));
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
                sb.AppendLine($" id:{p.id}:{p.user_name} @ {p.Position} z:{p.ZLevel} s:{p.TransitState}");
            }

            Console.Write(sb.ToString());
        }
    }
}
