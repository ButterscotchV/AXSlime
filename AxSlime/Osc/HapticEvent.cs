using AxSlime.Axis;

namespace AxSlime.Osc
{
    public readonly record struct HapticEvent
    {
        public readonly NodeBinding Node;
        public readonly float? Intensity;
        public readonly float? Duration;

        public HapticEvent(NodeBinding node, float? intensity = null, float? duration = null)
        {
            Node = node;
            Intensity = intensity;
            Duration = duration;
        }
    }
}
