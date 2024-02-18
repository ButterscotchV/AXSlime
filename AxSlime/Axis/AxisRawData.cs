using System.Numerics;

namespace Axis.DataTypes
{
    public class AxisNodeData
    {
        public bool isActive = false;
        public Quaternion rotation;
        public Vector3 acceleration;
    }

    public class AxisHubData
    {
        public bool isActive = false;
        public Quaternion rotation;
        public Vector3 position;
    }

    public class AxisOutputData
    {
        public const int NodesCount = 16;
        public readonly AxisNodeData[] nodesData = new AxisNodeData[NodesCount];
        public readonly AxisHubData hubData = new();

        public AxisOutputData()
        {
            for (var i = 0; i < NodesCount; i++)
            {
                nodesData[i] = new AxisNodeData();
            }
        }
    }
}
