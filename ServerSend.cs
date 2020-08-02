using System;
using System.Collections.Generic;
using System.Text;

namespace TFIServer
{
    class ServerSend
    {
        private static void SendTCPData(int _toClient, Packet packet)
        {
            packet.WriteLength();
            Server.clients[_toClient].tcp.SendData(packet);
        }

        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            foreach (var client in Server.clients)
            {
                client.Value.tcp.SendData(packet);
            }
        }

        private static void SendTCPDataToAll(int _exceptClient, Packet packet)
        {
            for (int _i = 0; _i <= Server.MaxPlayers; _i++)
            {
                if (_i != _exceptClient)
                {
                    Server.clients[_i].tcp.SendData(packet);
                }
            }
        }

        public static void Welcome(int _toClient, string _msg)
        {
            using(Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }
    }
}
