using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    class GameLogic
    {
        public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        long last_ticks = 0;

        public void AddPlayer(int _id, string _playerName)
        {
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

            //ServerSend.PlayerPosition(_newPlayer);
            //ServerSend.PlayerRotation(_newPlayer);

            Console.WriteLine($"+ [{_playerName}] accepted as player {_id} @ {_newPlayer.position}.");
        }
        public void UpdateFixed(long ticks)
        {
            var actions = ThreadManager.ExternalUpdate(this);
            if (actions == 0)
            {
                var ms_delta = (ticks - last_ticks) / TimeSpan.TicksPerMillisecond;
                if (ms_delta > 5000)
                {
                    Console.WriteLine($"ena: {ticks / TimeSpan.TicksPerSecond}");
                    last_ticks = ticks;
                }
                return;
            }

            foreach (Player _p in players.Values)
            {
                _p.Update(this);
            }

            last_ticks = ticks;
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
            ServerSend.Welcome(_id, "TFI-Hello");
        }

        internal void Disconnect(int _id)
        {
            _ = players.Remove(_id);
            Console.WriteLine($"+ player {_id} disconnected.");
        }

        internal Vector3 GetSpawnPoint()
        {

            Vector3 _point = Vector3.Zero;
            bool redo;

            do
            {
                redo = false;
                foreach (Player _p in players.Values)
                {
                    var delta = _point - _p.position;
                    if (_p.Hit(_point))
                    {
                        _point = _p.position + new Vector3(Constants.SPAWN_DIST_X, Constants.SPAWN_DIST_Y, 0);
                        redo = true;
                        break;
                    }
                }

            } while (redo);

            return _point;
        }

        internal void DumpPlayers() 
        {
            StringBuilder sb = new StringBuilder(120);
            foreach (var _p in players.Values)
            {
                sb.AppendLine($"player {_p.id} : {_p.username} @ {_p.position}");
            }

            Console.Write(sb.ToString());
        }
    }
}
