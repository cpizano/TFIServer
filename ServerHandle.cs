using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has  wrong client ID ({_clientIdCheck})!");
            }
            else
            {
                Server.clients[_fromClient].SendIntoGame(_username);

                var ip = Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint;
                Console.WriteLine($"{ip} [{_username}] accepted as player {_fromClient}.");
            }

        }

        public static void PlayerMovement(int _fromClient, Packet _packet)
        {
            bool[] _inputs = new bool[_packet.ReadInt()];
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = _packet.ReadBool();
            }
            Quaternion _rotation = _packet.ReadQuaternion();

            Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
        }
    }
}
