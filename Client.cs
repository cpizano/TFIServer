using System;
using System.Net;
using System.Net.Sockets;


namespace TFIServer
{
    class Client
    {
        public static int data_buffer_size = 4096;

        public readonly int id;
        public readonly TCP tcp;
        public readonly UDP udp;
        
        public Client(int _id)
        {
            id = _id;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            private Packet received_data;
            private byte[] received_bytes;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.SendBufferSize = data_buffer_size;
                socket.ReceiveBufferSize = data_buffer_size;

                stream = socket.GetStream();

                received_data = new Packet();
                received_bytes = new byte[data_buffer_size];

                stream.BeginRead(received_bytes, 0, data_buffer_size, new AsyncCallback(ReceiveCallback), null);

                Console.WriteLine($"= client {id} via {socket.Client.RemoteEndPoint}");

                ThreadManager.ExecuteOnMainThread((GameLogic game) =>
                {
                    game.Connect(id);
                });
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {ex}.");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLenght = stream.EndRead(result);
                    if (byteLenght <= 0)
                    {
                        Server.Disconnect(id);
                        return;
                    }

                    byte[] _data = new byte[byteLenght];
                    Array.Copy(received_bytes, _data, byteLenght);
                    received_data.Reset(HandleData(_data));

                    stream.BeginRead(
                        received_bytes, 0, data_buffer_size, new AsyncCallback(ReceiveCallback), null);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving data: {ex}.");
                    Server.Disconnect(id);
                }
            }

            private bool HandleData(byte[] data)
            {
                int packet_len = 0;

                received_data.SetBytes(data);

                if (received_data.UnreadLength() >= 4)
                {
                    packet_len = received_data.ReadInt();
                    if (packet_len <= 0)
                    {
                        return true;
                    }
                }

                while (packet_len > 0 && packet_len <= received_data.UnreadLength())
                {
                    byte[] bytes = received_data.ReadBytes(packet_len);
                    ThreadManager.ExecuteOnMainThread((GameLogic game) =>
                    {
                        using (var packet = new Packet(bytes))
                        {
                            int packet_id = packet.ReadInt();
                            Server.packetHandlers[packet_id](game, id, packet);
                        }
                    });

                    packet_len = 0;
                    if (received_data.UnreadLength() >= 4)
                    {
                        packet_len = received_data.ReadInt();
                        if (packet_len <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packet_len <= 1)
                {
                    return true;
                }

                return false;
            }
            public void Disconnect()
            {
                socket.Close();
                stream = null;
                received_data = null;
                received_bytes = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endpoint;

            private readonly int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endpoint)
            {
                // Called after receiving the first 'dummy' udp packet from client.
                endpoint = _endpoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(endpoint, packet);
            }

            public void HandleData(Packet packet_data)
            {
                int packet_len = packet_data.ReadInt();
                byte[] bytes = packet_data.ReadBytes(packet_len);

                ThreadManager.ExecuteOnMainThread((GameLogic game) =>
                {
                    using (Packet packet = new Packet(bytes))
                    {
                        int packet_id = packet.ReadInt();
                        Server.packetHandlers[packet_id](game, id, packet);
                    }
                });
            }
            public void Disconnect()
            {
                endpoint = null;
            }
        }

        public void Disconnect()
        {
            tcp.Disconnect();
            udp.Disconnect();

            ThreadManager.ExecuteOnMainThread((GameLogic game) =>
            {
                game.Disconnect(id);
            });
        }
    }
}

