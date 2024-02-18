using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace AxSlime.Slime
{
    public class SlimeUdpSocket : IDisposable
    {
        private bool _isRunning = false;

        private readonly IPEndPoint _slimeEndPoint;
        private UdpClient? _slimeClient;

        private ulong _packetNum = 0;
        private byte[] _buffer = new byte[128];

        public SlimeUdpSocket(IPEndPoint? slimeEndPoint = null)
        {
            _slimeEndPoint = slimeEndPoint ?? new IPEndPoint(IPAddress.Loopback, 6969);
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _packetNum = 0;
            _slimeClient = new UdpClient(0);
            _slimeClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );
            _slimeClient.Connect(_slimeEndPoint);

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _slimeClient?.Close();
            _slimeClient = null;
            _packetNum = 0;

            _isRunning = false;
        }

        public void SendPacket(SlimePacket packet)
        {
            var len = SerializePacket(_buffer, packet);
            _slimeClient?.Send(_buffer, len);
        }

        public int SerializePacket(Span<byte> buffer, SlimePacket packet)
        {
            // Skip forward for the packet size
            var i = sizeof(ushort);

            BinaryPrimitives.WriteUInt32BigEndian(buffer[i..], packet.PacketId);
            i += sizeof(uint);
            BinaryPrimitives.WriteUInt64BigEndian(buffer[i..], _packetNum++);
            i += sizeof(ulong);
            i += packet.Serialize(buffer[i..]);

            // Go back and write the final packet size
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)(i - sizeof(ushort)));

            return i;
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
