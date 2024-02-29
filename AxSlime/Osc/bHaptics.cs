using AxSlime.Axis;
using LucHeart.CoreOSC;

namespace AxSlime.Osc
{
    public class bHaptics : HapticsSource
    {
        public static readonly string bHapticsPrefix = "bHapticsOSC_";

        private static readonly Dictionary<string, NodeBinding[]> _mappings =
            new()
            {
                { "Vest_Front", [NodeBinding.Chest, NodeBinding.Hips] },
                { "Vest_Back", [NodeBinding.Chest, NodeBinding.Hips] },
                { "Arm_Left", [NodeBinding.LeftUpperArm, NodeBinding.LeftForeArm] },
                { "Arm_Right", [NodeBinding.RightUpperArm, NodeBinding.RightForeArm] },
                {
                    "Foot_Left",
                    [NodeBinding.LeftFoot, NodeBinding.LeftCalf, NodeBinding.LeftThigh]
                },
                {
                    "Foot_Right",
                    [NodeBinding.RightFoot, NodeBinding.RightCalf, NodeBinding.RightThigh]
                },
                { "Hand_Left", [NodeBinding.LeftHand] },
                { "Hand_Right", [NodeBinding.RightHand] },
                { "Head", [NodeBinding.Head] },
            };

        private static readonly Dictionary<string, HapticEvent[]> _eventMap = _mappings
            .Select(m => (m.Key, m.Value.Select(n => new HapticEvent(n)).ToArray()))
            .ToDictionary();

        public HapticEvent[] ComputeHaptics(string parameter, OscMessage message)
        {
            var bHaptics = parameter[bHapticsPrefix.Length..];
            foreach (var binding in _eventMap)
            {
                if (bHaptics.StartsWith(binding.Key))
                    return binding.Value;
            }

            return [];
        }

        public bool IsSource(string parameter, OscMessage message)
        {
            return parameter.StartsWith(bHapticsPrefix);
        }
    }
}
