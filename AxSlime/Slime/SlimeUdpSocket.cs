using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace AxSlime.Slime
{
    public class SlimeUdpSocket : IDisposable
    {
        // Offset of packet type + packet num (always 0)
        public static readonly int PacketHeaderLen = sizeof(uint) + sizeof(ulong);

        private bool _isRunning = false;
        private bool _isConnected = false;

        private readonly IPEndPoint _slimeEndPoint;
        private UdpClient? _slimeClient;

        private ulong _packetNum = 0;
        private byte[] _txBuffer = new byte[128];

        private CancellationTokenSource? _cancelTokenSource;
        private Task? _rxTask;

        public bool IsRunning => _isRunning;
        public bool IsConnected => _isConnected;

        public SlimeUdpSocket(IPEndPoint? slimeEndPoint = null)
        {
            _slimeEndPoint = slimeEndPoint ?? new IPEndPoint(IPAddress.Loopback, 6969);
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _packetNum = 0;
            _cancelTokenSource = new CancellationTokenSource();

            _slimeClient = new UdpClient(0);
            _slimeClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );
            _slimeClient.Connect(_slimeEndPoint);

            _rxTask = RxData(_cancelTokenSource.Token);

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _cancelTokenSource?.Cancel();

            _rxTask?.Wait();
            _rxTask = null;

            _cancelTokenSource?.Dispose();
            _cancelTokenSource = null;

            _slimeClient?.Close();
            _slimeClient = null;

            _packetNum = 0;

            _isConnected = false;
            _isRunning = false;
        }

        public void SendPacket(SlimePacket packet)
        {
            lock (_txBuffer)
            {
                var len = SerializePacket(_txBuffer, packet);
                // This should never happen
                if (len <= 0)
                    return;

                _slimeClient?.Send(_txBuffer, len);
            }
        }

        private int SerializePacket(Span<byte> buffer, SlimePacket packet)
        {
            var i = 0;

            i += packet.SerializePacketId(buffer[i..]);
            BinaryPrimitives.WriteUInt64BigEndian(buffer[i..], _packetNum++);
            i += sizeof(ulong);
            i += packet.Serialize(buffer[i..]);

            return i;
        }

        protected void OnRxData(byte[] data)
        {
            var packetType = (SlimeRxPacketType)SlimePacket.DeserializePacketId(data);
            var packetData = data.AsSpan()[PacketHeaderLen..];

            switch (packetType)
            {
                case SlimeRxPacketType.Heartbeat:
                    SendPacket(new Packet0Heartbeat());
                    break;
                case SlimeRxPacketType.PingPong:
                    var packet = new Packet10PingPong();
                    packet.Deserialize(packetData);
                    SendPacket(packet);
                    break;
            }
        }

        private async Task RxData(CancellationToken cancelToken = default)
        {
            while (!cancelToken.IsCancellationRequested && _slimeClient != null)
            {
                var slimeClient = _slimeClient;
                try
                {
                    var result = await slimeClient.ReceiveAsync(cancelToken);

                    if (result.Buffer.Length < PacketHeaderLen)
                        continue;
                    OnRxData(result.Buffer);
                    _isConnected = true;
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
