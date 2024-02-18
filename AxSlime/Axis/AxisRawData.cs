using System.Numerics;

namespace AxSlime.Axis
{
    public class AxisNodeData
    {
        private Quaternion _rotation;

        public AxisNodeData(int nodeId)
        {
            NodeId = nodeId;
        }

        public int NodeId { get; }
        public bool IsActive { get; set; } = false;
        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = Quaternion.Normalize(value);
        }
        public Vector3 Acceleration { get; set; }

        public override string ToString()
        {
            return $"rotation: {Rotation}, acceleration: {Acceleration}";
        }
    }

    public class AxisHubData
    {
        private Quaternion _rotation;

        public bool IsActive { get; set; } = false;
        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = Quaternion.Normalize(value);
        }
        public Vector3 Position { get; set; }

        public override string ToString()
        {
            return $"rotation: {Rotation}, position: {Position}";
        }
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
                nodesData[i] = new AxisNodeData(i);
            }
        }
    }
}
