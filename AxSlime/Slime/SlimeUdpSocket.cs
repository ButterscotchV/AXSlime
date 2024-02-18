using System.Net;
using System.Net.Sockets;

namespace AxSlime.Slime
{
    public class SlimeUdpSocket : IDisposable
    {
        private bool _isRunning = false;

        private readonly IPEndPoint _slimeEndPoint;
        private UdpClient? _slimeClient;

        public SlimeUdpSocket(IPEndPoint? slimeEndPoint = null)
        {
            _slimeEndPoint = slimeEndPoint ?? new IPEndPoint(IPAddress.Loopback, 6969);
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _slimeClient = new UdpClient(0);
            _slimeClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _slimeClient?.Close();
            _slimeClient = null;

            _isRunning = false;
        }

        public void SendPacket(SlimePacket packet) { }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
