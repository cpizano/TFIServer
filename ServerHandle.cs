using System;
using System.Collections.Generic;
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
                var ip = Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint;
                Console.WriteLine($"{ip} [{_username}] accepted as player {_fromClient}.");
            }

            // TODO: send player into game
        }
    }
}
