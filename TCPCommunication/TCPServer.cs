using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class TCPServer
    {
        public static int BufferSize = 4;

        private TcpListener tcpServer;
        private byte[] buffer = new byte[BufferSize];

        private readonly List<Socket> clients = new List<Socket>();
        private int bytesReceived;
        private int messageSize = -1;

        public TCPServer(int port)
        {
            // Make the listener listen for connections on any network device
            ConnectAsync(IPAddress.Any, port);
        }

        private void ListenForNewClients()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var tcpClient = await tcpServer.AcceptTcpClientAsync();

                    clients.Add(tcpClient.Client);

                    tcpClient.Client.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, tcpClient.Client);
                }
            });
        }

        public string Append { get; set; } = string.Empty;

        public event EventHandler<string> DataReceived;

        public bool IsConnected { get; }
        public bool IsSending { get; private set; }
        public string Status { get; }

        public bool ConnectAsync(IPAddress address, int port)
        {
            try
            {
                tcpServer = new TcpListener(address, port);
                tcpServer.Start();
                ListenForNewClients();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void DisconnectAsync()
        {
            tcpServer.Stop();
            tcpServer.Server.Disconnect(true);
        }

        public void SendAsync(string data)
        {
            WriteAsync(Encoding.ASCII.GetBytes(data.ToString()), data.ToString().Length);
        }

        public void WriteAsync(byte[] data, int length)
        {
            IsSending = true;

            foreach (var tcpClient in clients)
            {
                try
                {
                    tcpClient.BeginSend(data, 0, length, SocketFlags.None, null, null);
                }
                catch
                {
                    clients.Remove(tcpClient);
                }
            }

            IsSending = false;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket tcpClient = ar.AsyncState as Socket;
            try
            {
                int count = tcpClient.EndReceive(ar);
                bytesReceived += count;

                if (messageSize == -1)//we are still reading the size of the data
                {
                    if (count == 0)
                        throw new ProtocolViolationException("The remote peer closed the connection while reading the message size.");
                    
                    if (bytesReceived == 4)//we have received the entire message size information
                    {
                        //read the size of the message
                        messageSize = BitConverter.ToInt32(buffer, 0);
                        if (messageSize < 0)
                        {
                            throw new ProtocolViolationException("The remote peer sent a negative message size.");
                        }

                        //we should do some size validation here also (e.g. restrict incoming messages to x bytes long)
                        buffer = new Byte[messageSize];
                        //reset the bytes received back to zero
                        //because we are now switching to reading the message body
                        bytesReceived = 0;
                    }

                    if (messageSize != 0)
                    {
                        //we need more data – could be more of the message size information
                        //or it could be the message body.  The only time we won’t need to
                        //read more data is if the message size == 0
                        tcpClient.BeginReceive(buffer,
                            bytesReceived, //offset where data can be written
                            buffer.Length - bytesReceived, //how much data can be read into remaining buffer
                            SocketFlags.None, ReceiveCallback, tcpClient);

                    }

                }

                else //we are reading the body of the message

                {

                    if (bytesReceived == messageSize) //we have the entire message

                    {
                        var data = Encoding.ASCII.GetString(buffer);
                        DataReceived?.Invoke(this, data);
                        messageSize = -1;
                        bytesReceived = 0;
                        buffer = new byte[4];
                        tcpClient.BeginReceive(buffer,
                        bytesReceived, //offset where data can be written
                        buffer.Length, //how much data can be read into remaining buffer
                        SocketFlags.None, ReceiveCallback, tcpClient);
                    }
                    else //need more data.
                    {

                        if (count == 0)

                            throw new ProtocolViolationException("The remote peer closed the connection before the entire message was received");

                        tcpClient.BeginReceive(buffer,

                            bytesReceived, //offset where data can be written

                            buffer.Length - bytesReceived, //how much data can be read into remaining buffer

                            SocketFlags.None, ReceiveCallback, tcpClient);

                    }

                }

            }
            catch
            {
                clients.Remove(tcpClient);
            }
        }
    }
}
