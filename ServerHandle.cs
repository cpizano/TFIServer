using System;
using System.Numerics;

namespace TFIServer
{
    // These methods should only be called from the game thread.
    class ServerHandle
    {
        public static int packets_recv_udp = 0;
        public static int packets_recv_tcp = 0;

        public static void WelcomeReceived(GameLogic _game, int _fromClient, Packet _packet)
        {
            packets_recv_tcp += 1;

            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has  wrong client ID ({_clientIdCheck})!");
            }
            else
            {
                _game.AddPlayer(_fromClient, _username);
            }
        }

        public static void PlayerMovement(GameLogic _game, int _fromClient, Packet _packet)
        {
            packets_recv_udp += 1;

            bool[] _inputs = new bool[_packet.ReadInt()];
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = _packet.ReadBool();
            }
            Quaternion _rotation = _packet.ReadQuaternion();

            _game.PlayerInput(_fromClient, _inputs, _rotation);
        }
    }
}
