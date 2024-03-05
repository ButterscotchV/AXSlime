using AxSlime.Axis;
using AxSlime.Config;
using LucHeart.CoreOSC;

namespace AxSlime.Osc
{
    public class AxHaptics : HapticsSource
    {
        public static readonly string AxHapticsPrefix = "VRCOSC/AXHaptics/";
        public static readonly string BinaryPrefix = "Touched";
        public static readonly string AnalogPrefix = "Proximity";

        private static readonly Dictionary<string, NodeBinding> _nameToNode =
            Enum.GetValues<NodeBinding>().ToDictionary(v => Enum.GetName(v)!);

        private readonly AxSlimeConfig _config;

        public AxHaptics(AxSlimeConfig config)
        {
            _config = config;
        }

        public HapticEvent[] ComputeHaptics(string parameter, OscMessage message)
        {
            var axHaptics = parameter[AxHapticsPrefix.Length..];
            if (_config.Haptics.EnableTouch && axHaptics.StartsWith(BinaryPrefix))
            {
                var trigger = message.Arguments[0] as bool?;
                if (trigger != true)
                    return [];

                if (_nameToNode.TryGetValue(axHaptics[BinaryPrefix.Length..], out var nodeVal))
                    return [new HapticEvent(nodeVal)];
            }
            else if (_config.Haptics.EnableProx && axHaptics.StartsWith(AnalogPrefix))
            {
                var proximity = message.Arguments[0] as float? ?? -1f;
                if (proximity <= _config.Haptics.ProxThreshold)
                    return [];

                proximity = float.Clamp(proximity, 0f, 1f);
                var scaledProx = _config.Haptics.NonlinearProx ? proximity * proximity : proximity;

                var intensity = float.Clamp(
                    _config.Haptics.ProxMinIntensity
                        + (scaledProx * _config.Haptics.ProxIntensityRange),
                    _config.Haptics.ProxMinIntensity,
                    _config.Haptics.ProxMaxIntensity
                );
                if (
                    intensity > 0f
                    && _nameToNode.TryGetValue(axHaptics[AnalogPrefix.Length..], out var nodeVal)
                )
                    return [new HapticEvent(nodeVal, intensity, _config.Haptics.ProxDurationS)];
            }

            return [];
        }

        public bool IsSource(string parameter, OscMessage message)
        {
            return _config.Haptics.EnableAxHaptics && parameter.StartsWith(AxHapticsPrefix);
        }
    }
}
