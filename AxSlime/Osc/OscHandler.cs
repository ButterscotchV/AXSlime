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

        private readonly AxisCommander _axisCommander;
        private readonly float _intensity;
        private readonly float _durationSeconds;

        private readonly UdpClient _oscClient;
        private readonly CancellationTokenSource _cancelTokenSource = new();
        private readonly Task _oscReceiveTask;

        public OscHandler(
            AxisCommander axisCommander,
            float intensity = 1f,
            float durationSeconds = 1f,
            IPEndPoint? ipEndPoint = null
        )
        {
            _axisCommander = axisCommander;
            _intensity = intensity;
            _durationSeconds = durationSeconds;
            _oscClient = new UdpClient(ipEndPoint ?? new IPEndPoint(IPAddress.Loopback, 9001));
            _oscReceiveTask = OscReceiveTask(_cancelTokenSource.Token);
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

            var node = GetNodeFromAddress(message.Address);
            if (node == null)
                return;

            var trigger = (bool?)message.Arguments[0] ?? false;
            if (trigger)
                _axisCommander.SetNodeVibration((byte)node, _intensity, _durationSeconds);
        }

        private static NodeBinding? GetNodeFromAddress(string address)
        {
            if (address.Length <= AvatarParamPrefix.Length)
                return null;

            switch (address[AvatarParamPrefix.Length..])
            {
                case "VRCOSC/AXHaptics/IsRightThighHapticActive":
                    return NodeBinding.RightThigh;
                case "VRCOSC/AXHaptics/IsRightCalfHapticActive":
                    return NodeBinding.RightCalf;
                case "VRCOSC/AXHaptics/IsLeftThighHapticActive":
                    return NodeBinding.LeftThigh;
                case "VRCOSC/AXHaptics/IsLeftCalfHapticActive":
                    return NodeBinding.LeftCalf;
                case "VRCOSC/AXHaptics/IsRightUpperArmHapticActive":
                    return NodeBinding.RightUpperArm;
                case "VRCOSC/AXHaptics/IsRightForearmHapticActive":
                    return NodeBinding.RightForeArm;
                case "VRCOSC/AXHaptics/IsLeftUpperArmHapticActive":
                    return NodeBinding.LeftUpperArm;
                case "VRCOSC/AXHaptics/IsLeftForearmHapticActive":
                    return NodeBinding.LeftForeArm;
                case "VRCOSC/AXHaptics/IsChestHapticActive":
                    return NodeBinding.Chest;
                default:
                    return null;
            }
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
