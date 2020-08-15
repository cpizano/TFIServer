using System;
using System.Collections.Generic;
using System.Numerics;


namespace TFIServer
{
    class GameLogic
    {
        public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

        public void AddPlayer(int _id, string _playerName)
        {
            var newPlayer = new Player(_id, _playerName, GetSpawnPoint());

            // Tell new player about all other existing players
            foreach (Player player in players.Values)
            {
                ServerSend.SpawnPlayer(newPlayer.id, player);
            }

            players[_id] = newPlayer;

            // Tell all players (including self) about new player,
            foreach (Player player in players.Values)
            {
                ServerSend.SpawnPlayer(player.id, newPlayer);
            }

            Console.WriteLine($"+ [{_playerName}] accepted as player {_id} @ {newPlayer.position}.");
        }
        public void UpdateFixed()
        {
            foreach (Player player in players.Values)
            {
                player.Update(this);
            }

            ThreadManager.UpdateFromNetwork(this);
        }

        public Vector3 UpdatePosition(Player player, Vector3 position)
        {
            foreach (Player _p in players.Values)
            {
                if (_p == player)
                {
                    continue;
                }

                if (_p.Hit(position))
                {
                    return player.position;
                }
            }

            return position;
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
    }
}
