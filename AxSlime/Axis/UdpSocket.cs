using System.Net;
using System.Net.Sockets;

namespace AxSlime.Axis
{
    public class UdpSocket : IDisposable
    {
        private static readonly IPAddress _rawAddr = new(new byte[] { 239, 255, 239, 172 });
        private static readonly IPAddress _messageAddr = new(new byte[] { 239, 255, 239, 174 });

        private bool _isRunning = false;
        private bool _isTxStarted = false;

        /// <summary>
        /// The server's endpoint.
        /// </summary>
        private readonly IPEndPoint _commandEndPoint;
        private UdpClient? _commandClient;

        private readonly IPEndPoint _multicastEndPoint;
        private readonly IPEndPoint _messageEndPoint;

        private CancellationTokenSource? _cancelTokenSource;

        private Task? _rawReceiveTask;
        private readonly byte[] _dataInBuffer = new byte[1024];
        private int _dataInLength = 0;

        private Task? _messageReceiveTask;
        private readonly byte[] _messageInBuffer = new byte[1024];
        private int _messageInLength = 0;

        public UdpSocket(
            IPEndPoint? commandEndPoint = null,
            int multicastPort = 45071,
            int messagePort = 45069
        )
        {
            _commandEndPoint = commandEndPoint ?? new IPEndPoint(IPAddress.Loopback, 45068);
            _multicastEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
            _messageEndPoint = new IPEndPoint(IPAddress.Any, messagePort);
        }

        protected Span<byte> DataIn =>
            _dataInLength > 0 ? _dataInBuffer.AsSpan(0, _dataInLength) : [];
        protected Span<byte> MessageIn =>
            _messageInLength > 0 ? _messageInBuffer.AsSpan(0, _messageInLength) : [];

        public bool IsRunning => _isRunning;
        public bool IsTxStarted => _isTxStarted;

        private static Socket CreateSocket(IPAddress addr, IPEndPoint endPoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(endPoint);
            socket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(addr, IPAddress.Any)
            );
            return socket;
        }

        public virtual void Start()
        {
            if (_isRunning)
                return;

            _cancelTokenSource = new CancellationTokenSource();

            // Create command client
            _commandClient = new UdpClient();
            _commandClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );

            _rawReceiveTask = ReceiveRawData(_cancelTokenSource.Token);
            _messageReceiveTask = ReceiveMessage(_cancelTokenSource.Token);

            _isRunning = true;
        }

        public virtual void Stop()
        {
            if (!_isRunning)
                return;

            _cancelTokenSource?.Cancel();

            _rawReceiveTask?.Wait();
            _rawReceiveTask = null;

            _messageReceiveTask?.Wait();
            _messageReceiveTask = null;

            _cancelTokenSource?.Dispose();
            _cancelTokenSource = null;

            _commandClient?.Close();
            _commandClient = null;

            _isRunning = false;
        }

        protected virtual void OnDataIn() { }

        protected virtual void OnMessageIn() { }

        public void SendData(byte[] data)
        {
            try
            {
                _commandClient?.Send(data, data.Length, _commandEndPoint);
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        private async Task ReceiveRawData(CancellationToken cancelToken = default)
        {
            using var socket = CreateSocket(_rawAddr, _multicastEndPoint);
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await socket.ReceiveFromAsync(
                        _dataInBuffer,
                        remoteEp,
                        cancelToken
                    );
                    _dataInLength = result.ReceivedBytes;
                    remoteEp = result.RemoteEndPoint;

                    if (_dataInLength <= 0)
                        continue;
                    _isTxStarted = true; // First data arrived so tx started
                    OnDataIn();
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        private async Task ReceiveMessage(CancellationToken cancelToken = default)
        {
            using var socket = CreateSocket(_messageAddr, _messageEndPoint);
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await socket.ReceiveFromAsync(
                        _messageInBuffer,
                        remoteEp,
                        cancelToken
                    );
                    _messageInLength = result.ReceivedBytes;
                    remoteEp = result.RemoteEndPoint;

                    if (_messageInLength <= 0)
                        continue;
                    OnMessageIn();
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
