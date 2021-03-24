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
    public sealed class SocketClient
    {
        private bool _connected;
        private readonly Socket _clientSocket;
        private readonly int _bufferLength;
        private readonly ILogger _logger;
        private readonly Encoding _encoding;
        private readonly ArrayPool<byte> _bufferPool;

        public SocketClient(
            int bufferSize,
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            Encoding encoding = null,
            ILogger logger = null
            )
        {
            _clientSocket = new Socket(addressFamily, socketType, protocolType);
            _bufferLength = bufferSize;
            _logger = logger ?? LoggerFactory.Create((_) => _.AddConsole()).CreateLogger<SocketClient>();
            _encoding = encoding ?? Encoding.UTF8;

            _connected = false;
            _bufferPool = ArrayPool<byte>.Create();
        }

        public async Task ConnectAsync(IPEndPoint endPoint)
        {
            try
            {
                await _clientSocket.ConnectAsync(endPoint);
                _logger.LogInformation("Connected successfully");
                _connected = true;
                _ = ListenForIncomingData();
            }
            catch (Exception e)
            {
                _connected = false;
            }
        }
        public async Task ConnectAsync(IPAddress address, int port)
        {
            try
            {
                await _clientSocket.ConnectAsync(address, port);
                _logger.LogInformation("Connected successfully");
                _connected = true;
                _ = ListenForIncomingData();
            }
            catch (Exception e)
            {
                _connected = false;
            }
        }
        public void Disconnect()
        {
            _connected = false;
            _clientSocket.Close();
        }
        private async Task ListenForIncomingData()
        {
            do
            {
                try
                {
                    byte[] buffer = _bufferPool.Rent(_bufferLength);
                    int readBytes = await _clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                    string message = _encoding.GetString(buffer[..readBytes]);
                    _logger.LogInformation($"Received: {message}");
                    _bufferPool.Return(buffer, true);
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Connection closed");
                }
            } while (_connected);
        }
        public async Task SendMessageAsync(string message)
        {
            try
            {
                byte[] buffer = _encoding.GetBytes(message);
                await _clientSocket.SendAsync(buffer, SocketFlags.None);
                _logger.LogInformation($"Sent: {message}");
            }
            catch (Exception e)
            {

            }
        }
    }
}
