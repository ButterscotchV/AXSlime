using System.Numerics;

namespace AxSlime.Axis
{
    public enum NodeBinding
    {
        RightThigh,
        RightCalf,
        LeftThigh,
        LeftCalf,
        RightUpperArm,
        RightForeArm,
        LeftUpperArm,
        LeftForeArm,
        Chest,
        RightFoot,
        LeftFoot,
        RightHand,
        LeftHand,
        RightShoulder,
        LeftShoulder,
        Head,
        Hips
    }

    public class ChangeTimeout<T>(T defaultValue, TimeSpan timeout)
    {
        private T _value = defaultValue;
        private bool _notDefault = false;

        public ChangeTimeout(T defaultValue)
            : this(defaultValue, TimeSpan.FromMinutes(1)) { }

        public T Value
        {
            get => _value;
            set
            {
                if (_notDefault && !Compare(value, _value))
                {
                    LastActive = DateTime.UtcNow;
                }

                _value = value;
                _notDefault = true;
            }
        }
        public TimeSpan Timeout { get; set; } = timeout;
        public DateTime LastActive { get; private set; } = DateTime.MinValue;
        public bool IsActive => DateTime.UtcNow - LastActive < Timeout;

        public bool Compare(T a, T b)
        {
            return Equals(a, b);
        }
    }

    public interface AxisTracker
    {
        public int TrackerId { get; }
        public bool HasMovement { get; }
        public bool IsActive { get; }
        public Quaternion Rotation { get; }
        public bool HasAcceleration => false;
        public Vector3 Acceleration => default;
        public bool HasPosition => false;
        public Vector3 Position => default;
    }

    public class AxisNodeData(int nodeId) : AxisTracker
    {
        private readonly ChangeTimeout<Quaternion> _rotation =
            new(Quaternion.Identity, TimeSpan.FromMinutes(1));
        private readonly ChangeTimeout<Vector3> _acceleration =
            new(default, TimeSpan.FromMinutes(1));

        public int NodeId { get; } = nodeId;
        public NodeBinding NodeBinding { get; } = (NodeBinding)nodeId;
        public int TrackerId => NodeId + 1;

        public bool HasMovement => _rotation.IsActive;
        public bool IsConnected { get; set; } = false;
        public bool IsActive => IsConnected || HasMovement;

        public Quaternion Rotation
        {
            get => _rotation.Value;
            set => _rotation.Value = Quaternion.Normalize(value);
        }
        public bool HasAcceleration => true;
        public Vector3 Acceleration
        {
            get => _acceleration.Value;
            set => _acceleration.Value = value;
        }

        public override string ToString()
        {
            return $"{{rotation: {Rotation}, acceleration: {Acceleration}}}";
        }
    }

    public class AxisHubData : AxisTracker
    {
        private readonly ChangeTimeout<Quaternion> _rotation =
            new(Quaternion.Identity, TimeSpan.FromMinutes(1));
        private readonly ChangeTimeout<Vector3> _position = new(default, TimeSpan.FromMinutes(1));

        public int TrackerId => 0;

        public bool HasMovement => _rotation.IsActive;
        public bool IsActive => HasMovement;

        public Quaternion Rotation
        {
            get => _rotation.Value;
            set => _rotation.Value = Quaternion.Normalize(value);
        }
        public bool HasPosition => true;
        public Vector3 Position
        {
            get => _position.Value;
            set => _position.Value = value;
        }

        public override string ToString()
        {
            return $"{{rotation: {Rotation}, position: {Position}}}";
        }
    }

    public class AxisOutputData
    {
        public const int NodesCount = 17;
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
