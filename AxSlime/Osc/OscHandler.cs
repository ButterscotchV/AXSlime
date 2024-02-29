using System.Net;
using System.Net.Sockets;
using System.Text;
using AxSlime.Axis;
using LucHeart.CoreOSC;

namespace AxSlime.Osc
{
    public class OscHandler : IDisposable
    {
        public static readonly string BundleAddress = "#bundle\0";
        public static readonly byte[] BundleAddressBytes = Encoding.ASCII.GetBytes(BundleAddress);
        public static readonly string AvatarParamPrefix = "/avatar/parameters/";

        public float HapticsIntensity;
        public float HapticsDurationSeconds;
        public bool UseAxHaptics;
        public bool UseBHaptics;

        private readonly AxHaptics _axHaptics = new();
        private readonly bHaptics _bHaptics = new();

        private readonly AxisCommander _axisCommander;
        private readonly UdpClient _oscClient;
        private readonly CancellationTokenSource _cancelTokenSource = new();
        private readonly Task _oscReceiveTask;

        public OscHandler(
            AxisCommander axisCommander,
            float intensity = 1f,
            float durationSeconds = 1f,
            IPEndPoint? ipEndPoint = null,
            bool useAxHaptics = true,
            bool useBHaptics = true
        )
        {
            HapticsIntensity = intensity;
            HapticsDurationSeconds = durationSeconds;
            UseAxHaptics = useAxHaptics;
            UseBHaptics = useBHaptics;

            _axisCommander = axisCommander;
            _oscClient = new UdpClient(ipEndPoint ?? new IPEndPoint(IPAddress.Loopback, 9001));
            _oscReceiveTask = OscReceiveTask(_cancelTokenSource.Token);
        }

        private static bool IsBundle(ReadOnlySpan<byte> buffer)
        {
            return buffer.Length > 16 && buffer[..8].SequenceEqual(BundleAddressBytes);
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

            var events = ComputeEvents(message);
            if (events.Length <= 0)
                return;

            foreach (var @event in events)
            {
                _axisCommander.SetNodeVibration(
                    (byte)@event.Node,
                    @event.Intensity ?? HapticsIntensity,
                    @event.Duration ?? HapticsDurationSeconds
                );
            }
        }

        private HapticEvent[] ComputeEvents(OscMessage message)
        {
            if (message.Address.Length <= AvatarParamPrefix.Length)
                return [];

            var param = message.Address[AvatarParamPrefix.Length..];

            if (UseAxHaptics && _axHaptics.IsSource(param, message))
                return _axHaptics.ComputeHaptics(param, message);

            if (UseBHaptics && _bHaptics.IsSource(param, message))
                return _bHaptics.ComputeHaptics(param, message);

            return [];
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
