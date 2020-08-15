using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TFIServer
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        
        public delegate void PacketHandler(GameLogic _game, int _fromClient, Packet _packet);

        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        private static ReaderWriterLockSlim clientLock = new ReaderWriterLockSlim();
        private static readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public static void Start(int _maxPlayers, int _port) 
        {
            MaxPlayers = _maxPlayers;
            Port = _port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);
        }

        public static void SendTCPData(int _toClient, Packet packet)
        {
            packet.WriteLength();
            ReadOperation(_toClient, (client) =>
            {
                client.tcp.SendData(packet);
            });
        }

        public static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            ReadOperation(_toClient, (client) =>
            {
                client.udp.SendData(_packet);
            });
        }

        public static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            ReadOperationAll(_exceptClient, (client) =>
            {
                client.tcp.SendData(_packet);
            });
        }

        public static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            ReadOperationAll(_exceptClient, (client) =>
            {
                client.udp.SendData(_packet);
            });
        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            clientLock.EnterWriteLock();
            try
            {
                foreach (var client in clients)
                {
                    if (client.Value.tcp.socket == null)
                    {
                        client.Value.tcp.Connect(_client);
                        return;
                    }
                }
            }
            finally
            {
                clientLock.ExitWriteLock();
            }

            Console.WriteLine($"Server is full!");
        }

        public static void Disconnect(int _toClient)
        {
            WriteOperation(_toClient, (client) =>
            {
                client.Disconnect();
            });
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        Console.WriteLine("Client is zero!");
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        // The first packet is the dummy packet the client sends right after hello that
                        // opens the port in the NAT? firewall? anyhow we don't process it.
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    // The comparsion needs to be deep, not that the refs are equal.
                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                    else
                    {
                        Console.WriteLine($"Error: Fake client {_clientId}");
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        private static void ReadOperation(int _toClient, Action<Client> _action)
        {
            clientLock.EnterReadLock();
            try
            {
                _action(Server.clients[_toClient]);
            }
            finally
            {
                clientLock.ExitReadLock();
            }
        }

        private static void ReadOperationAll(int _exceptClient, Action<Client> _action)
        {
            clientLock.EnterReadLock();
            try
            {
                for (int i = 1; i <= Server.MaxPlayers; i++)
                {
                    if (i != _exceptClient)
                    {
                        _action(Server.clients[i]);
                    }
                }
            }
            finally
            {
                clientLock.ExitReadLock();
            }
        }

        private static void WriteOperation(int _toClient, Action<Client> action)
        {
            clientLock.EnterWriteLock();
            try
            {
                action(Server.clients[_toClient]);
            }
            finally
            {
                clientLock.ExitWriteLock();
            }
        }

        private static void WriteOperationAll(int _exceptClient, Action<Client> _action)
        {
            clientLock.EnterWriteLock();
            try
            {
                for (int i = 1; i <= Server.MaxPlayers; i++)
                {
                    if (i != _exceptClient)
                    {
                        _action(Server.clients[i]);
                    }
                }
            }
            finally
            {
                clientLock.ExitWriteLock();
            }
        }

        private static void InitializeServerData()
        {
            clientLock.EnterWriteLock();
            try
            {
                for (int i = 1; i <= MaxPlayers; i++)
                {
                    clients.Add(i, new Client(i));
                }
            }
            finally
            {
                clientLock.ExitWriteLock();
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement }
            };
        }
    }

}
