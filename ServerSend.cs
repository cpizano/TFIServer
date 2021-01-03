
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TFIServer
{
    // These methods are called from multiple threads. Don't add state here.
    class ServerSend
    {
        public static int version = 0;

        #region Packets
        public static void Welcome(int _toClient, int ppu, MapHandler map)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(ServerSend.version);
                _packet.Write(ServerHandle.version);
                _packet.Write(_toClient);  // becomes client id.
                _packet.Write(ppu);  // Pixels per unit of distance.
                _packet.Write(map.mapVersion);
                _packet.Write(map.Layers);
                _packet.Write(map.Row_count);
                _packet.Write(map.Column_count);

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void MapLayerRow(int _toClient, int layer, int row, int row_len,
            IEnumerable<MapCell> cells)
        {
            using (var _packet = new Packet((int)ServerPackets.mapLayerRow))
            {
                _packet.Write(layer);
                _packet.Write(row);
                _packet.Write(row_len);
                foreach(var cell in cells)
                {
                    _packet.Write(cell.tile);
                    _packet.Write(cell.rle);
                }

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void SpawnPlayer(int _toClient, Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.user_name);
                _packet.Write(_player.Position);
                _packet.Write(_player.ZLevel);
                _packet.Write(_player.Health);

                Server.SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player _player, int z_boost = 0)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.Position);
                _packet.Write(z_boost > 0 ? z_boost : _player.ZLevel);

                Server.SendUDPDataToAll(0, _packet);
            }
        }

        public static void PlayerHealth(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.Health);

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
        // line numbers. Last was 104, before 115 and before 103.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InitProtocolVersion()
        {
            // this comment makes it 104 so we don't collide an older version.
            version = Constants.GetLineNumer();
        }
    }
}
