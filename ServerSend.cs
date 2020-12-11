﻿
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TFIServer
{
    // These methods are called from multiple threads. Don't add state here.
    class ServerSend
    {
        public static int version = 0;

        #region Packets
        public static void Welcome(int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(ServerSend.version);
                _packet.Write(ServerHandle.version);
                _packet.Write(_toClient);  // becomes client id.
                _packet.Write(MapHandler.mapVersion);
                _packet.Write(MapHandler.layers);
                _packet.Write(MapHandler.rows);
                _packet.Write(MapHandler.column_count);

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void MapLayerRow(int _toClient, int layer, int row, int row_len,
            IEnumerable<short> cells)
        {
            using (var _packet = new Packet((int)ServerPackets.mapLayerRow))
            {
                _packet.Write(layer);
                _packet.Write(row);
                _packet.Write(row_len);
                foreach(var cell in cells)
                {
                    _packet.Write(cell);
                }

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void SpawnPlayer(int _toClient, Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                Server.SendUDPDataToAll(0, _packet);
            }
        }

        public static void PlayerRotation(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.rotation);

                Server.SendUDPDataToAll(_player.id, _packet);
            }
        }

        public static void PlayerQuit(Player _player, int reason)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerQuit))
            {
                _packet.Write(_player.id);
                _packet.Write(reason);

                Server.SendTCPDataToAll(_player.id, _packet);
            }
        }

        #endregion

        // Keep this last. It controls the protocol version via cheeky
        // line numbers. Last was 99.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InitProtocolVersion()
        {
            version = Constants.GetLineNumer();
        }
    }
}
