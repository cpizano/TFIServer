using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TFIServer
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public TCP tcp;
        
        public Client(int _id)
        {
            id = _id;
            tcp = new TCP(id);
        }

        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
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
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(ReceiveCallback), null);

                // TODO: send hello packet.
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int _byteLenght = stream.EndRead(result);
                    if (_byteLenght < 0)
                    {
                        // TODO: disconnect.
                        return;
                    }

                    byte[] _data = new byte[_byteLenght];
                    Array.Copy(receiveBuffer, _data, _byteLenght);

                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(ReceiveCallback), null);

                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving data: {_ex}.");
                    // TODO: disconnect client.
                }
            }
        }
    }
}
