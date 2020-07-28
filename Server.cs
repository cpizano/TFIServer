using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TFIServer
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        private static TcpListener tcpListener;

        public static void Start(int _maxPlayers, int _port) 
        {
            MaxPlayers = _maxPlayers;
            Port = _port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Server started on {Port}.");
        }
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint} ...");

            foreach (var client in clients)
            {
                if (client.Value.tcp.socket == null)
                {
                    client.Value.tcp.Connect(_client);
                    return;
                }
            }

            Console.WriteLine($"Server is full!");
        }

        private static void InitializeServerData()
        {
            for (int i = 0; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }
        }
    }

}
