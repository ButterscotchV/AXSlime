using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Axis.DataTypes;

namespace Axis.Communication
{
    public class AxisRuntimeUdpSocket : UdpSocket
    {
        public AxisOutputData AxisOutputData = new();

        //Data Packet Characteristics
        private const int DataStartOffset = 6;
        private const int NodeIndexOffset = 1;
        private const int DataSize = 15;
        private const int DataPacketSizeInBytes = 290;

        public event EventHandler<AxisOutputData>? OnAxisData;

        public AxisRuntimeUdpSocket(
            IPEndPoint? commandEndPoint = null,
            int multicastPort = 45071,
            int messagePort = 45069
        )
            : base(commandEndPoint, multicastPort, messagePort)
        {
            OnDataIn += ProcessData;
        }

        private void ProcessData(object? source, EventArgs e)
        {
            if (DataIn.Length != DataPacketSizeInBytes)
                return;

            GetDataFromHub(AxisOutputData);
            GetDataFromNodes(AxisOutputData);

            OnAxisData?.Invoke(this, AxisOutputData);
        }

        private static int GetAxisIndex(int i, int dataIndex)
        {
            return DataStartOffset + (i * DataSize) + (dataIndex * sizeof(short)) + NodeIndexOffset;
        }

        private float ReadShort(int i, int dataIndex)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(DataIn[GetAxisIndex(i, dataIndex)..]);
        }

        private float ReadQuatAxis(int i, int dataIndex)
        {
            return ReadShort(i, dataIndex) * 0.00006103f;
        }

        private float ReadAccelAxis(int i, int dataIndex)
        {
            return ReadShort(i, dataIndex) * 0.00390625f;
        }

        private void GetDataFromNodes(AxisOutputData axisOutputData)
        {
            for (var i = 0; i < AxisOutputData.NodesCount; i++)
            {
                var node = axisOutputData.nodesData[i];

                var dataIndex = 0;

                var x = ReadQuatAxis(i, dataIndex++);
                var z = ReadQuatAxis(i, dataIndex++);
                var y = ReadQuatAxis(i, dataIndex++);
                var w = ReadQuatAxis(i, dataIndex++);
                node.rotation = new Quaternion(x, y, z, w);

                var xAccel = ReadAccelAxis(i, dataIndex++);
                var yAccel = ReadAccelAxis(i, dataIndex++);
                var zAccel = ReadAccelAxis(i, dataIndex++);
                node.acceleration = new Vector3(xAccel, yAccel, zAccel);
            }
        }

        private float ReadHubAxis(int start, int dataIndex)
        {
            return BinaryPrimitives.ReadSingleLittleEndian(
                DataIn[(start + (dataIndex * sizeof(float)))..]
            );
        }

        private void GetDataFromHub(AxisOutputData axisOutputData)
        {
            var startingPosition = DataIn.Length - 28;
            var dataIndex = 0;

            var x = ReadHubAxis(startingPosition, dataIndex++);
            var y = ReadHubAxis(startingPosition, dataIndex++);
            var z = ReadHubAxis(startingPosition, dataIndex++);
            var w = ReadHubAxis(startingPosition, dataIndex++);
            axisOutputData.hubData.rotation = new Quaternion(x, y, z, w);

            var xPos = ReadHubAxis(startingPosition, dataIndex++);
            var yPos = ReadHubAxis(startingPosition, dataIndex++);
            var zPos = ReadHubAxis(startingPosition, dataIndex++);
            axisOutputData.hubData.position = new Vector3(-xPos, yPos, zPos);
        }
    }
}
