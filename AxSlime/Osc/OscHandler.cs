using System.Net;
using System.Net.Sockets;
using System.Text;
using AxSlime.Axis;
using LucHeart.CoreOSC;

namespace AxSlime.Osc
{
    public class OscHandler : IDisposable
    {
        public static readonly string AvatarParamPrefix = "/avatar/parameters/";
        public static readonly string bHapticsPrefix = "bHapticsOSC_";

        private readonly AxisCommander _axisCommander;
        private readonly float _intensity;
        private readonly float _durationSeconds;

        private readonly UdpClient _oscClient;
        private readonly CancellationTokenSource _cancelTokenSource = new();
        private readonly Task _oscReceiveTask;

        private readonly bool _useBHaptics;

        public OscHandler(
            AxisCommander axisCommander,
            float intensity = 1f,
            float durationSeconds = 1f,
            IPEndPoint? ipEndPoint = null,
            bool useBHaptics = true
        )
        {
            _axisCommander = axisCommander;
            _intensity = intensity;
            _durationSeconds = durationSeconds;

            _oscClient = new UdpClient(ipEndPoint ?? new IPEndPoint(IPAddress.Loopback, 9001));
            _oscReceiveTask = OscReceiveTask(_cancelTokenSource.Token);

            _useBHaptics = useBHaptics;
        }

        private static bool IsBundle(ReadOnlySpan<byte> buffer)
        {
            return buffer.Length > 16 && Encoding.ASCII.GetString(buffer[..8]) == "#bundle\0";
        }

        private async Task OscReceiveTask(CancellationToken cancelToken = default)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var packet = await _oscClient.ReceiveAsync(cancelToken);
                    if (IsBundle(packet.Buffer))
                    {
                        var bundle = OscBundle.ParseBundle(packet.Buffer);
                        if (bundle.Timestamp > DateTime.Now)
                        {
                            // Wait for the specified timestamp
                            _ = Task.Run(
                                async () =>
                                {
                                    await Task.Delay(bundle.Timestamp - DateTime.Now, cancelToken);
                                    OnOscBundle(bundle);
                                },
                                cancelToken
                            );
                        }
                        else
                        {
                            OnOscBundle(bundle);
                        }
                    }
                    else
                    {
                        OnOscMessage(OscMessage.ParseMessage(packet.Buffer));
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        private void OnOscBundle(OscBundle bundle)
        {
            foreach (var message in bundle.Messages)
            {
                OnOscMessage(message);
            }
        }

        private void OnOscMessage(OscMessage message)
        {
            if (message.Arguments.Length <= 0)
                return;

            var trigger = message.Arguments[0] as bool? ?? false;
            if (!trigger)
                return;

            var nodes = GetNodesFromAddress(message.Address);
            if (nodes == null)
                return;

            foreach (var node in nodes)
            {
                _axisCommander.SetNodeVibration((byte)node, _intensity, _durationSeconds);
            }
        }

        private NodeBinding[]? GetNodesFromAddress(string address)
        {
            if (address.Length <= AvatarParamPrefix.Length)
                return null;

            var param = address[AvatarParamPrefix.Length..];
            switch (param)
            {
                case "VRCOSC/AXHaptics/IsRightThighHapticActive":
                    return [NodeBinding.RightThigh];
                case "VRCOSC/AXHaptics/IsRightCalfHapticActive":
                    return [NodeBinding.RightCalf];
                case "VRCOSC/AXHaptics/IsLeftThighHapticActive":
                    return [NodeBinding.LeftThigh];
                case "VRCOSC/AXHaptics/IsLeftCalfHapticActive":
                    return [NodeBinding.LeftCalf];
                case "VRCOSC/AXHaptics/IsRightUpperArmHapticActive":
                    return [NodeBinding.RightUpperArm];
                case "VRCOSC/AXHaptics/IsRightForearmHapticActive":
                    return [NodeBinding.RightForeArm];
                case "VRCOSC/AXHaptics/IsLeftUpperArmHapticActive":
                    return [NodeBinding.LeftUpperArm];
                case "VRCOSC/AXHaptics/IsLeftForearmHapticActive":
                    return [NodeBinding.LeftForeArm];
                case "VRCOSC/AXHaptics/IsChestHapticActive":
                    return [NodeBinding.Chest];
            }

            if (_useBHaptics && param.StartsWith(bHapticsPrefix))
            {
                var bHaptics = param[bHapticsPrefix.Length..];
                if (bHaptics.StartsWith("Vest_Front") || bHaptics.StartsWith("Vest_Back"))
                    return [NodeBinding.Chest, NodeBinding.Hips];
                else if (bHaptics.StartsWith("Arm_Left"))
                    return [NodeBinding.LeftUpperArm];
                else if (bHaptics.StartsWith("Arm_Right"))
                    return [NodeBinding.RightUpperArm];
                else if (bHaptics.StartsWith("Foot_Left"))
                    return [NodeBinding.LeftFoot, NodeBinding.LeftCalf, NodeBinding.LeftThigh];
                else if (bHaptics.StartsWith("Foot_Right"))
                    return [NodeBinding.RightFoot, NodeBinding.RightCalf, NodeBinding.RightThigh];
                else if (bHaptics.StartsWith("Hand_Left"))
                    return [NodeBinding.LeftHand, NodeBinding.LeftForeArm];
                else if (bHaptics.StartsWith("Hand_Right"))
                    return [NodeBinding.RightHand, NodeBinding.RightForeArm];
                else if (bHaptics.StartsWith("Head"))
                    return [NodeBinding.Head];
            }

            return null;
        }

        public void Dispose()
        {
            _cancelTokenSource.Cancel();
            _oscReceiveTask.Wait();
            _cancelTokenSource.Dispose();
            _oscClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
