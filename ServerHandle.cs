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

        public static void WelcomeReceived(GameLogic game, int from_client, Packet packet)
        {
            packets_recv_tcp += 1;

            int _clientIdCheck = packet.ReadInt();
            string _username = packet.ReadString();

            if (from_client != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {from_client}) has  wrong client ID ({_clientIdCheck})!");
            }
            else
            {
                game.AddPlayer(from_client, _username);
            }
        }

        public static void PlayerMovement(GameLogic game, int from_client, Packet packet)
        {
            packets_recv_udp += 1;

            bool[] _inputs = new bool[packet.ReadInt()];
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = packet.ReadBool();
            }
            Quaternion _rotation = packet.ReadQuaternion();

            game.PlayerInput(from_client, _inputs, _rotation);
        }

        public static void SessionEnd(GameLogic game, int from_client, Packet packet)
        {
            packets_recv_tcp += 1;
            string _reason = packet.ReadString();

            Console.WriteLine($"Player {from_client} quit [{_reason}]");

            game.PlayerQuit(from_client);
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
