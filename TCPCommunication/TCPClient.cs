using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class TCPClient
    {
        public static int BufferSize = 1024 * 1024 * 5;
        private TcpClient client = new TcpClient();
        private bool isConnected;
        private bool isSending;
        private byte[] buffer = new byte[BufferSize];

        public event EventHandler<bool> IsConnectedChanged;
        public event EventHandler<bool> IsSendingChanged;

        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                if (isConnected == value) return;
                isConnected = value;
                IsConnectedChanged?.Invoke(this, value);
            }
        }

        public bool IsSending
        {
            get { return isSending; }
            set
            {
                if (isSending == value) return;
                isSending = value;
                IsSendingChanged?.Invoke(this, value);
            }
        }

        public string Append { get; set; } = string.Empty;

        public async Task DisconnectAsync() => await Task.Run(() => Disconnect());
        
        public void SendAsync(string data)
        {
            var d = data + Append;
            byte[] message = Encoding.ASCII.GetBytes(d);
            byte[] lengthPrefix = BitConverter.GetBytes(d.Length);
            byte[] ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);
            client.Client.BeginSend(ret, 0, ret.Length, SocketFlags.None, null, null);
        }

        public bool Connect(IPAddress address, int port)
        {
            if (client == null)
            {
                client = new TcpClient();
            }

            var result = client.BeginConnect(address, port, null, null);
            IsConnected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            if (IsConnected)
            {
                client.Client.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback,
                    client.Client);
            }

            return IsConnected;
        }

        public void Disconnect()
        {

            client.Client.Disconnect(false);
            client = null;
            IsConnected = false;
        }

        public async Task WriteAsync(byte[] data, int length)
        {
            IsSending = true;
            await client.GetStream().WriteAsync(data, 0, length);
            IsSending = false;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var tcpClient = ar.AsyncState as Socket;

                var bytesRead = tcpClient.EndReceive(ar);

                if (bytesRead == 0)
                {
                    tcpClient.EndReceive(ar);
                }

                var data = Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd();

                buffer = new byte[BufferSize];
                tcpClient.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, tcpClient);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //ignore this exception, it's intended operation:
                //To cancel a pending call to the BeginConnect() method, close the Socket. 
                //When the Close() method is called while an asynchronous operation is in progress, 
                //the callback provided to the BeginConnect() method is called. A subsequent call 
                //to the EndConnect(IAsyncResult) method will throw an ObjectDisposedException 
                //to indicate that the operation has been cancelled.
            }
        }

    }
}
