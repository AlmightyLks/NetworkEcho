using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkEcho.SocketEcho
{
    class Program
    {
        static async Task Main()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 8000);
            SocketEchoServer echoServer = new SocketEchoServer(
                endpoint,
                1024,
                SocketType.Stream,
                ProtocolType.Tcp
                );
            echoServer.Start();

            SocketClient client = new SocketClient(
                1024,
                SocketType.Stream,
                ProtocolType.Tcp
                );

            await client.ConnectAsync(endpoint);
            await client.SendMessageAsync("Good evening");
            await Task.Delay(-1);
        }
    }
}
