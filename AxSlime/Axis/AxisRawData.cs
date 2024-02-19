using System.Numerics;

namespace AxSlime.Axis
{
    public interface AxisTracker
    {
        public int TrackerId { get; }
        public bool IsActive { get; }
        public Quaternion Rotation { get; }
        public bool HasAcceleration => false;
        public Vector3 Acceleration { get; }
        public bool HasPosition => false;
        public Vector3 Position { get; }
    }

    public class AxisNodeData : AxisTracker
    {
        private Quaternion _rotation = Quaternion.Identity;

        public AxisNodeData(int nodeId)
        {
            NodeId = nodeId;
        }

        public int NodeId { get; }
        public int TrackerId => NodeId + 1;
        public bool IsActive { get; set; } = false;
        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = Quaternion.Normalize(value);
        }
        public bool HasAcceleration => true;
        public Vector3 Acceleration { get; set; } = default;
        public Vector3 Position { get; set; } = default;

        public override string ToString()
        {
            return $"{{rotation: {Rotation}, acceleration: {Acceleration}}}";
        }
    }

    public class AxisHubData : AxisTracker
    {
        private Quaternion _rotation = Quaternion.Identity;

        public int TrackerId => 0;
        public bool IsActive { get; set; } = false;
        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = Quaternion.Normalize(value);
        }
        public Vector3 Acceleration { get; set; } = default;
        public bool HasPosition => true;
        public Vector3 Position { get; set; } = default;

        public override string ToString()
        {
            return $"{{rotation: {Rotation}, position: {Position}}}";
        }
    }

    public class AxisOutputData
    {
        public const int NodesCount = 16;
        public const int TrackerCount = NodesCount + 1;
        public readonly AxisNodeData[] nodesData = new AxisNodeData[NodesCount];
        public readonly AxisHubData hubData = new();

        public AxisOutputData()
        {
            for (var i = 0; i < NodesCount; i++)
            {
                nodesData[i] = new AxisNodeData(i);
            }
        }

        public override string ToString()
        {
            return $"{{hubData: {hubData}, nodesData: [{string.Join<AxisNodeData>(", ", nodesData)}]}}";
        }
    }
}
