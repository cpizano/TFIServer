using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace TFIServer
{
    // These methods should only be called from the game thread.
    class ServerHandle
    {
        public static int version = 0;

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

        public static void SessionEnd(GameLogic _game, int _fromClient, Packet _packet)
        {
            packets_recv_tcp += 1;
            string _reason = _packet.ReadString();

            Console.WriteLine($"Player {_fromClient} quit [{_reason}]");

            _game.PlayerQuit(_fromClient);
        }

        // Keep this last. It controls the protocol version via cheecky
        // line numbers. Last was 61.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InitProtocolVersion()
        {
            version = Constants.GetLineNumer();
        }
    }

}
