using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketEcho
{
    class Program
    {
        static async Task Main()
        {
            SocketEchoServer echoServer = new SocketEchoServer(
                new IPEndPoint(IPAddress.Loopback, 8000),
                1024,
                SocketType.Stream,
                ProtocolType.Tcp
                );
            echoServer.Start();

            await Task.Delay(-1);
        }
    }
}
