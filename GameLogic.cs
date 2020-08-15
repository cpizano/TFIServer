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
            var newPlayer = new Player(_id, _playerName, new Vector3(0, 0, 0));

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

            Console.WriteLine($"+ [{_playerName}] accepted as player {_id}.");
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
            return position;
        }

        internal void Connect(int _id)
        {
            ServerSend.Welcome(_id, "TFI-Hello");
        }

        internal void Disconnect(int _id)
        {
            players.Remove(_id);
            Console.WriteLine($"+ player {_id} disconnected.");
        }
    }

}
