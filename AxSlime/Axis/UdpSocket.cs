using System.Net;
using System.Net.Sockets;

namespace Axis.Communication
{
    public class UdpSocket
    {
        public bool isTxStarted = false;

        private string commandIP = "127.0.0.1"; // local host

        private int rxPort = 45069; // port to receive data from Python on

        private int txPort = 45068; // port to send data to Python on

        // Create necessary UdpClient objects
        private UdpClient _commandClient;
        private IPEndPoint _commandRemoteEndPoint;

        protected Thread RawDataReceiveThread; // Receiving Thread
        protected byte[] DataInBytes;
        protected bool DataWaitingForProcessing = false;
        private UdpClient _dataClient;

        protected Thread MessageReceiveThread;
        protected byte[] MessageInBytes;
        protected bool MessageWaitingForProcessing = false;

        public void SendData(byte[] data)
        {
            try
            {
                _commandClient?.Send(data, data.Length, _commandRemoteEndPoint);
            }
            catch (Exception err)
            {
                if (err is ObjectDisposedException)
                {
                    //Debug.Log("Got it");
                }
                else
                {
                    Console.Error.WriteLine(err.ToString());
                }
            }
        }

        int multicastPort = 45071;

        //int _rawDataPort = 45071;
        IPEndPoint _rawDataIp;
        IPEndPoint _messageDataIp;

        protected void StartReceiveThread()
        {
            CreateCommandUpdClient();
            // Create a new thread for reception of incoming messages
            var rawDataSocket = CreateRawDataSocket();
            StartRawDataReceiveThread(rawDataSocket);

            var messageSocket = CreateMessageReceivingSocket();
            StartMessageReceiveThread(messageSocket);
        }

        private void CreateCommandUpdClient()
        {
            _commandRemoteEndPoint = new IPEndPoint(IPAddress.Parse(commandIP), txPort);
            _commandClient = new UdpClient();
            _commandClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );
        }

        private void StartRawDataReceiveThread(Socket rawDataSocket)
        {
            RawDataReceiveThread = new Thread(() => ReceiveRawData(rawDataSocket));
            RawDataReceiveThread.IsBackground = true;
            RawDataReceiveThread.Start();
        }

        private void StartMessageReceiveThread(Socket messageSocket)
        {
            MessageReceiveThread = new Thread(() => ReceiveMessage(messageSocket));
            MessageReceiveThread.IsBackground = true;
            MessageReceiveThread.Start();
        }

        private Socket CreateMessageReceivingSocket()
        {
            var messageAddress = IPAddress.Parse("239.255.239.174");
            _messageDataIp = new IPEndPoint(IPAddress.Any, rxPort);
            var messageSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp
            );
            messageSocket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );

            messageSocket.Bind(_messageDataIp);
            messageSocket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(messageAddress, IPAddress.Any)
            );
            return messageSocket;
        }

        private Socket CreateRawDataSocket()
        {
            var rawDataGroupAddress = IPAddress.Parse("239.255.239.172");
            _rawDataIp = new IPEndPoint(IPAddress.Any, multicastPort);
            var rawDataSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp
            );
            rawDataSocket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );
            rawDataSocket.Bind(_rawDataIp);
            rawDataSocket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(rawDataGroupAddress, IPAddress.Any)
            );
            return rawDataSocket;
        }

        // Receive data, update packets received
        private void ReceiveRawData(Socket socket)
        {
            // Debug.Log("Test");
            while (RawDataReceiveThread != null && RawDataReceiveThread.IsAlive)
            {
                try
                {
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

                    var buffer = new byte[1024];
                    var bytesRead = socket.ReceiveFrom(buffer, ref remoteEp);

                    if (bytesRead <= 0)
                        continue;

                    DataInBytes = new byte[bytesRead];
                    Array.Copy(buffer, DataInBytes, bytesRead);
                    DataWaitingForProcessing = true;

                    if (!isTxStarted) // First data arrived so tx started
                    {
                        isTxStarted = true;
                    }

                    //ProcessInput(dataInBytes);
                }
                catch (Exception err)
                {
                    if (err is ThreadAbortException) { }
                    else
                    {
                        Console.Error.WriteLine(err.ToString());
                    }
                }
            }
        }

        private void ReceiveMessage(Socket socket)
        {
            while (MessageReceiveThread != null && MessageReceiveThread.IsAlive)
            {
                //try
                //{
                //    EndPoint remoteEp = new IPEndPoint(IPAddress.Parse("239.255.239.174"), rxPort);
                //    var debugBuffer = new byte[] { 0, 1, 2, 3, 4, 5 };
                //    socket.SendTo(debugBuffer, remoteEp);
                //}
                //catch (Exception err)
                //{
                //    if (err is ThreadAbortException)
                //    {
                //    }
                //    else
                //    {
                //        print(err.ToString());
                //    }
                //}
                try
                {
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

                    var buffer = new byte[1024];

                    var bytesRead = socket.ReceiveFrom(buffer, ref remoteEp);
                    //Debug.Log("Getting message");
                    if (bytesRead <= 0)
                        continue;

                    MessageInBytes = new byte[bytesRead];
                    Array.Copy(buffer, MessageInBytes, bytesRead);
                    MessageWaitingForProcessing = true;
                }
                catch (Exception err)
                {
                    if (err is ThreadAbortException) { }
                    else
                    {
                        Console.Error.WriteLine(err.ToString());
                    }
                }
            }
        }

        //Prevent crashes - close clients and threads properly!
        protected void StopReceivingThread()
        {
            if (RawDataReceiveThread != null)
            {
                RawDataReceiveThread.Abort();
            }
            if (MessageReceiveThread != null)
            {
                MessageReceiveThread.Abort();
            }

            if (_commandClient != null)
            {
                _commandClient.Close();
            }
        }
    }
}
