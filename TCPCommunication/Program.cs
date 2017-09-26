using System;
using System.Net;

namespace TCPCommunication
{
    class Program
    {
        private static TCPClient client;

        static void Main(string[] args)
        {
            var server = new TCPServer(8888);
            server.DataReceived += Program_DataReceived;

            client = new TCPClient();
            client.Connect(IPAddress.Parse("127.0.0.1"), 8888);

            while (true)
            {
                var s = Console.ReadLine().TrimEnd().ToLower();

                if (s == "disconnect")
                {
                    client.Disconnect();
                }
                else if (s == "sdisconnect")
                {
                    //server.DisconnectAsync();
                }
                else if (s == "connect")
                {
                    client.Connect(IPAddress.Parse("127.0.0.1"), 8888);
                }
                else if (s == "spam")
                {
                    Spam();
                }
                else
                {
                    client.SendAsync(s);
                }
            }
        }

        private static void Spam()
        {
            for (int i = 0; i < 10; i++)
            {
                client.SendAsync(i.ToString());
            }

            client.SendAsync("10");
            client.SendAsync("9");
            client.SendAsync("8");
            client.SendAsync("7");
            client.SendAsync("6");
            client.SendAsync("5");
            client.SendAsync("4");
            client.SendAsync("3");
            client.SendAsync("2");
            client.SendAsync("1");
            client.SendAsync("0");
        }

        private static void Program_DataReceived(object sender, string e)
        {
            Console.WriteLine($"Server Rx: {e}");
        }
    }
}
