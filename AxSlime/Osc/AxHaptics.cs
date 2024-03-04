using AxSlime.Axis;
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

        public HapticEvent[] ComputeHaptics(string parameter, OscMessage message)
        {
            var axHaptics = parameter[AxHapticsPrefix.Length..];
            if (axHaptics.StartsWith(BinaryPrefix))
            {
                if (_nameToNode.TryGetValue(axHaptics[BinaryPrefix.Length..], out var nodeVal))
                    return [new HapticEvent(nodeVal)];
            }
            else if (axHaptics.StartsWith(AnalogPrefix))
            {
                if (_nameToNode.TryGetValue(axHaptics[AnalogPrefix.Length..], out var nodeVal))
                    return [new HapticEvent(nodeVal)];
            }

            return [];
        }

        public bool IsSource(string parameter, OscMessage message)
        {
            return parameter.StartsWith(AxHapticsPrefix);
        }
    }
}
