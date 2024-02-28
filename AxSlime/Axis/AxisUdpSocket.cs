using System.Buffers.Binary;
using System.Net;
using System.Numerics;

namespace AxSlime.Axis
{
    public class AxisUdpSocket : UdpSocket
    {
        public readonly AxisOutputData AxisOutputData = new();
        public readonly AxisCommander AxisRuntimeCommander;

        // Data packet details
        /// <summary>
        /// 6 bytes, unsure what this is
        /// </summary>
        private const int DataStartOffset = 6;

        /// <summary>
        /// 1 byte
        /// </summary>
        private const int NodeIndexOffset = sizeof(byte);

        /// <summary>
        /// 15 bytes
        /// </summary>
        private const int NodeDataSize = NodeIndexOffset + (sizeof(short) * 7);

        /// <summary>
        /// 28 bytes
        /// </summary>
        private const int HubDataSize = sizeof(float) * 7;

        /// <summary>
        /// 290 bytes, unsure why there is an extra byte than the full data, it must be
        /// between node data and hub data... Maybe it's a hub data index?
        /// </summary>
        private const int DataPacketSize =
            DataStartOffset + (NodeDataSize * AxisOutputData.NodesCount) + 1 + HubDataSize;

        public event EventHandler<AxisOutputData>? OnAxisData;

        public AxisUdpSocket(
            IPEndPoint? commandEndPoint = null,
            int multicastPort = 45071,
            int messagePort = 45069
        )
            : base(commandEndPoint, multicastPort, messagePort)
        {
            AxisRuntimeCommander = new(this);
        }

        public override void Start()
        {
            if (IsRunning)
                return;
            base.Start();
            AxisRuntimeCommander.StartStreaming();
        }

        public override void Stop()
        {
            if (!IsRunning)
                return;
            AxisRuntimeCommander.StopStreaming();
            base.Stop();
        }

        protected override void OnDataIn()
        {
            if (DataIn.Length != DataPacketSize)
                return;

            var packetData = DataIn[DataStartOffset..];
            GetDataFromNodes(packetData, AxisOutputData);
            GetDataFromHub(packetData, AxisOutputData);

            OnAxisData?.Invoke(this, AxisOutputData);
        }

        private static float ReadQuatAxis(ReadOnlySpan<byte> data)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(data) * 0.00006103f;
        }

        private static float ReadAccelAxis(ReadOnlySpan<byte> data)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(data) * 0.00390625f;
        }

        private static int GetDataFromNodes(ReadOnlySpan<byte> data, AxisOutputData axisOutputData)
        {
            var i = 0;
            for (var j = 0; j < AxisOutputData.NodesCount; j++)
            {
                var node = axisOutputData.nodesData[j];

                // Index offset
                var index = data[i];
                i += NodeIndexOffset;
                node.IsConnected = Convert.ToBoolean(index & 0b10000000);

                var x = ReadQuatAxis(data[i..]);
                i += sizeof(short);
                var z = ReadQuatAxis(data[i..]);
                i += sizeof(short);
                var y = ReadQuatAxis(data[i..]);
                i += sizeof(short);
                var w = ReadQuatAxis(data[i..]);
                i += sizeof(short);
                node.Rotation = new Quaternion(x, y, z, w);

                var xAccel = ReadAccelAxis(data[i..]);
                i += sizeof(short);
                var yAccel = ReadAccelAxis(data[i..]);
                i += sizeof(short);
                var zAccel = ReadAccelAxis(data[i..]);
                i += sizeof(short);
                node.Acceleration = new Vector3(xAccel, yAccel, zAccel);
            }

            return i;
        }

        private static int GetDataFromHub(ReadOnlySpan<byte> data, AxisOutputData axisOutputData)
        {
            var hub = axisOutputData.hubData;

            var hubData = data[(data.Length - HubDataSize)..];
            var i = 0;

            var x = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            var y = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            var z = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            var w = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            hub.Rotation = new Quaternion(x, y, z, w);

            var xPos = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            var yPos = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            var zPos = BinaryPrimitives.ReadSingleLittleEndian(hubData[i..]);
            i += sizeof(float);
            hub.Position = new Vector3(-xPos, yPos, zPos);

            return i;
        }
    }
}
