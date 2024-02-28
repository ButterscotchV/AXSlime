using System.Net;
using System.Numerics;
using AxSlime.Axis;
using AxSlime.Slime;

namespace AxSlime.Bridge
{
    public class BridgeController : IDisposable
    {
        // 90 degree offset in the X (pitch) axis, this is to match IMU axes
        public static readonly Quaternion AxesOffset = Quaternion.CreateFromAxisAngle(
            new Vector3(1f, 0f, 0f),
            MathF.PI / 2f
        );

        private readonly IPEndPoint? _slimeEndPoint;

        public BridgeController(AxisOutputData axisTrackers, IPEndPoint? slimeEndPoint = null)
        {
            AxisTrackers = axisTrackers;
            _slimeEndPoint = slimeEndPoint;
        }

        public AxisOutputData AxisTrackers { get; set; }
        public SlimeUdpSocket[] SlimeTrackers { get; } =
            new SlimeUdpSocket[AxisOutputData.TrackerCount];

        public void Update()
        {
            UpdateTracker(AxisTrackers.hubData);
            foreach (var axis in AxisTrackers.nodesData)
            {
                UpdateTracker(axis);
            }
        }

        private void UpdateTracker(AxisTracker axis)
        {
            var slime = (SlimeTrackers[axis.TrackerId] ??= new SlimeUdpSocket(_slimeEndPoint));
            if (!axis.IsActive)
            {
                slime.Stop();
                return;
            }
            else if (!slime.IsRunning)
            {
                RegisterTracker(axis, slime);
            }

            // Quaternion left to right conversion
            Quaternion slimeQuat = new Quaternion(
                -axis.Rotation.X,
                axis.Rotation.Y,
                axis.Rotation.Z,
                axis.Rotation.W
            );

            slime.SendPacket(new Packet17RotationData() { Rotation = AxesOffset * slimeQuat });

            if (axis.HasAcceleration)
            {
                slime.SendPacket(new Packet4Accel() { Acceleration = axis.Acceleration });
            }
        }

        private static void RegisterTracker(AxisTracker axis, SlimeUdpSocket slime)
        {
            slime.Start();
            slime.SendPacket(
                new Packet3Handshake()
                {
                    MacAddress = PacketUtils.IncrementedMacAddress(
                        PacketUtils.DefaultMacAddress,
                        axis.TrackerId
                    )
                }
            );
            slime.SendPacket(new Packet15SensorInfo());
        }

        public void Dispose()
        {
            foreach (var slime in SlimeTrackers)
            {
                slime?.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
