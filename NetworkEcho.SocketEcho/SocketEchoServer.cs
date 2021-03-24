using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEcho.SocketEcho
{
    public sealed class SocketEchoServer
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
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            ILogger logger = null)
        {
            _serverSocket = new Socket(addressFamily, socketType, protocolType);
            _bufferSize = bufferSize;
            _endPoint = endpoint;
            _logger = logger ?? LoggerFactory.Create((_) => _.AddConsole()).CreateLogger<SocketEchoServer>();

            _cancelled = false;
            _connectedSockets = new List<Socket>();
            ConnectedSockets = _connectedSockets.AsReadOnly();
            _bufferPool = ArrayPool<byte>.Create();
        }

        public void Start(int backlog = 1)
        {
            try
            {
                _serverSocket.Bind(_endPoint);
                _serverSocket.Listen(backlog);
                _ = ListenForIncomingConnections();
            }
            catch (Exception e)
            {

            }
        }
        public void Stop()
        {
            try
            {
                _cancelled = true;
                _serverSocket.Close();
            }
            catch (Exception e)
            {

            }
        }
        private async Task ListenForIncomingConnections()
        {
            do
            {
                try
                {
                    Socket acceptedSocket = await _serverSocket.AcceptAsync();
                    _logger.LogInformation("Connection accepted!");
                    _ = AcceptDataAsync(acceptedSocket);
                }
                catch (Exception e)
                {
                    _cancelled = true;
                }
            } while (!_cancelled);
        }
        private async Task AcceptDataAsync(Socket socket)
        {
            do
            {
                try
                {
                    byte[] buffer = _bufferPool.Rent(_bufferSize);
                    int readBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    _logger.LogInformation("Data received!");
                    _ = EchoDataAsync(socket, buffer[..readBytes]);
                    _bufferPool.Return(buffer);
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Connection closed");
                    break;
                }
            } while (!_cancelled);
        }
        private async Task EchoDataAsync(Socket socket, byte[] data)
        {
            try
            {
                await socket.SendAsync(data, SocketFlags.None);
                _logger.LogInformation("Echo'ed data!");
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Failed to echo data:\n{e}");
            }
        }
    }
}
