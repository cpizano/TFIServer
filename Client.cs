﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace TFIServer
{
    class Client
    {
        public static int dataBufferSize = 4096;

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
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.SendBufferSize = dataBufferSize;
                socket.ReceiveBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(ReceiveCallback), null);

                Console.WriteLine($"= client {id} via {socket.Client.RemoteEndPoint}");

                ThreadManager.ExecuteOnMainThread((GameLogic _game) =>
                {
                    _game.Connect(id);
                });
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                } 
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}.");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int _byteLenght = stream.EndRead(result);
                    if (_byteLenght <= 0)
                    {
                        Server.Disconnect(id);
                        return;
                    }

                    byte[] _data = new byte[_byteLenght];
                    Array.Copy(receiveBuffer, _data, _byteLenght);
                    receivedData.Reset(HandleData(_data));

                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(ReceiveCallback), null);

                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving data: {_ex}.");
                    Server.Disconnect(id);
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread((GameLogic _game) =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            Server.packetHandlers[_packetId](_game, id, _packet);
                        }
                    });

                    _packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (_packetLength <= 1)
                {
                    return true;
                }

                return false;
            }
            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private readonly int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                // Called after receiving the first 'dummy' udp packet from client.
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread((GameLogic _game) =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](_game, id, _packet);
                    }
                });
            }
            public void Disconnect()
            {
                endPoint = null;
            }
        }

        public void Disconnect()
        {
            tcp.Disconnect();
            udp.Disconnect();

            ThreadManager.ExecuteOnMainThread((GameLogic _game) =>
            {
                _game.Disconnect(id);
            });
        }
    }
}

