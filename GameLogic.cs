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
        private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        private long last_ticks = 0;
        private GameLogicOptions options = 0;
        private static MapHandler mapHandler;

        // The client must have the same ppu value.
        private readonly int pixels_per_unit = 32;

        public GameLogic()
        {
            mapHandler = new MapHandler(pixels_per_unit);
            mapHandler.LoadMapJSON("..\\..\\map002.json");
        }

        public void AddPlayer(int _id, string _playerName)
        {
            // First lets send the map.
            mapHandler.SendMap(_id);

            var _newPlayer = new Player(_id, _playerName, GetSpawnPoint());

            // Tell new player about all other existing players
            foreach (Player player in players.Values)
            {
                ServerSend.SpawnPlayer(_newPlayer.id, player);
            }

            players[_id] = _newPlayer;

            // Tell all players (including self) about new player,
            foreach (Player player in players.Values)
            {
                ServerSend.SpawnPlayer(player.id, _newPlayer);
            }

            Console.WriteLine($"+ [{_playerName}] accepted as player {_id} @ {_newPlayer.position}.");
        }
        public void UpdateFixed(long ticks)
        {
            var actions = ThreadManager.ExternalUpdate(this);
            if (actions == 0)
            {
                if (options.HasHeartbeat())
                {
                    var ms_delta = (ticks - last_ticks) / TimeSpan.TicksPerMillisecond;
                    if (ms_delta > 5000)
                    {
                        var seconds = ticks / TimeSpan.TicksPerSecond;
                        Console.WriteLine($"{seconds} ena {ServerHandle.packets_recv_tcp} {ServerHandle.packets_recv_udp}");
                        last_ticks = ticks;
                    }
                }
                return;
            }

            foreach (Player _p in players.Values)
            {
                _p.Update(this);
            }

            last_ticks = ticks;
        }

        internal void PlayerQuit(int _fromClient)
        {

            if (!players.TryGetValue(_fromClient, out var _player))
            {
                return;
            }

            _ = players.Remove(_fromClient);
            ServerSend.PlayerQuit(_player, 0);
        }

        internal void PlayerInput(int _fromClient, bool[] _inputs, Quaternion _rotation)
        {
            players[_fromClient].SetInput(_inputs, _rotation);
        }

        internal void MovePlayer(Player _player, Vector2 _inputDirection)
        {

            // For 3D, Z is forward (towards screen) and +Y is up.
            // Vector3 _forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            // Vector3 _right = Vector3.Normalize(Vector3.Cross(_forward, new Vector3(0, 1, 0)));

            Vector3 _forward = Vector3.UnitY;
            Vector3 _right = -Vector3.UnitX;

            Vector3 _moveDirection = (_right * _inputDirection.X) + (_forward * _inputDirection.Y);
            var newPosition = _player.position + (_moveDirection * _player.moveSpeed);

            foreach (Player _p in players.Values)
            {
                if (_p == _player)
                {
                    continue;
                }

                if (_p.Hit(newPosition))
                {
                    // On hit we don't move player.
                    return;
                }
            }

            _player.position = newPosition;
            ServerSend.PlayerPosition(_player);
            // Client is authoritative for rotation: update not sent back to self.
            ServerSend.PlayerRotation(_player);

        }

        internal void Connect(int _id)
        {
            ServerSend.Welcome(_id, pixels_per_unit, mapHandler);
        }

        internal void Disconnect(int _id)
        {
            PlayerQuit(_id);
            Console.WriteLine($"+ player {_id} disconnected.");
        }

        internal void ToggleHeartbeatPrint()
        {
            if (options.HasHeartbeat())
            {
                options &= ~GameLogicOptions.Heartbeat;
            }
            else
            {
                options |= GameLogicOptions.Heartbeat;
            }
        }

        internal Vector3 GetSpawnPoint()
        {
            foreach (var spawn in mapHandler.GetPlayerSpawns())
            {
                foreach (var player in players.Values)
                {
                    if (spawn.Contains(player.position.X, player.position.Y))
                    {
                        goto found;
                    }
                }

                // Nobody in this spawn point, use it.
                return GetMidRect(spawn);

            found:;
            }

            return Vector3.Zero;
        }

        internal Vector3 GetMidRect(RectangleF r)
        {
            return new Vector3((r.X + r.Width / 2), (r.Y + r.Height / 2), 0);
        }

        internal void DumpPlayers() 
        {
            if (players.Count == 0)
            {
                Console.WriteLine("no players");
                return;
            }

            StringBuilder sb = new StringBuilder(120);
            foreach (var _p in players.Values)
            {
                sb.AppendLine($"   player {_p.id} : {_p.username} @ {_p.position}");
            }

            Console.Write(sb.ToString());
        }
    }
}
