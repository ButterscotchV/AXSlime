using System.Net.Sockets;
using System.Text;
using AxSlime.Axis;
using AxSlime.Config;
using LucHeart.CoreOSC;

namespace AxSlime.Osc
{
    public class OscHandler : IDisposable
    {
        public static readonly string BundleAddress = "#bundle\0";
        public static readonly byte[] BundleAddressBytes = Encoding.ASCII.GetBytes(BundleAddress);
        public static readonly string AvatarParamPrefix = "/avatar/parameters/";

        private readonly AxSlimeConfig _config;
        private readonly AxisCommander _axisCommander;
        private readonly UdpClient _oscClient;

        private readonly CancellationTokenSource _cancelTokenSource = new();
        private readonly Task _oscReceiveTask;

        private readonly AxHaptics _axHaptics;
        private readonly bHaptics _bHaptics;

        private readonly HapticsSource[] _hapticsSources;

        public OscHandler(AxSlimeConfig config, AxisCommander axisCommander)
        {
            _config = config;
            _axisCommander = axisCommander;
            _oscClient = new UdpClient(config.OscReceiveEndPoint);
            _oscReceiveTask = OscReceiveTask(_cancelTokenSource.Token);

            _axHaptics = new(config);
            _bHaptics = new(config);

            _hapticsSources = [_axHaptics, _bHaptics];
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

            foreach (var @event in ComputeEvents(message))
            {
                _axisCommander.SetNodeVibration(
                    (byte)@event.Node,
                    @event.Intensity ?? _config.Haptics.TouchIntensity,
                    @event.Duration ?? _config.Haptics.TouchDurationS
                );
            }
        }

        private HapticEvent[] ComputeEvents(OscMessage message)
        {
            if (message.Address.Length <= AvatarParamPrefix.Length)
                return [];

            var param = message.Address[AvatarParamPrefix.Length..];
            foreach (var source in _hapticsSources)
            {
                if (source.IsSource(param, message))
                    return source.ComputeHaptics(param, message);
            }

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
