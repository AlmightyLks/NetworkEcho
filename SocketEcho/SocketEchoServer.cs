using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketEcho
{
    public class SocketEchoServer
    {
        public IReadOnlyList<Socket> ConnectedSockets { get; }
        private List<Socket> _connectedSockets;
        private readonly Socket _serverSocket;
        private readonly ArrayPool<byte> _bufferPool;
        private readonly int _bufferSize;
        private readonly IPEndPoint _endPoint;
        private readonly ILogger _logger;
        private bool _cancelled;

        public SocketEchoServer(
            IPEndPoint endpoint,
            int bufferSize,
            SocketType socketType,
            ProtocolType protocolType,
            ILogger logger = null)
        {
            _serverSocket = new Socket(socketType, protocolType);
            _bufferSize = bufferSize;
            _endPoint = endpoint;
            _logger = logger ?? new Logger<SocketEchoServer>(new LoggerFactory());

            _cancelled = false;
            _connectedSockets = new List<Socket>();
            ConnectedSockets = _connectedSockets.AsReadOnly();
        }
        public void Start(int backlog = 1)
        {
            _serverSocket.Bind(_endPoint);
            _serverSocket.Listen(backlog);
            new Task(async () => await ListenForIncomingConnections()).Start();
        }
        private async Task ListenForIncomingConnections()
        {
            do
            {
                Socket acceptedSocket = await _serverSocket.AcceptAsync();
                _logger.LogInformation("Connection accepted!");
                new Task(async () => await AcceptDataAsync(acceptedSocket)).Start();
            } while (!_cancelled);
        }
        private async Task AcceptDataAsync(Socket socket)
        {
            do
            {
                byte[] buffer = _bufferPool.Rent(_bufferSize);
                int readBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
                _logger.LogInformation("Data received!");
                new Task(async () => await EchoDataAsync(socket, buffer[..readBytes])).Start();
            } while (!_cancelled);
        }
        private async Task EchoDataAsync(Socket socket, byte[] data)
        {
            await socket.SendAsync(data, SocketFlags.None);
            _logger.LogInformation("Echo'ed data!");
        }
    }
}
