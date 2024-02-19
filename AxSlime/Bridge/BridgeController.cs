using AxSlime.Axis;
using AxSlime.Slime;

namespace AxSlime.Bridge
{
    public class BridgeController : IDisposable
    {
        public BridgeController(AxisOutputData axisTrackers)
        {
            AxisTrackers = axisTrackers;
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
            var slime = (SlimeTrackers[axis.TrackerId] ??= new SlimeUdpSocket());
            if (!slime.IsRunning)
            {
                RegisterTracker(axis, slime);
            }

            slime.SendPacket(new Packet17RotationData() { Rotation = axis.Rotation });

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
