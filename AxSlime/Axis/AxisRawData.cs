using System.Numerics;

namespace Axis.DataTypes
{
    public class AxisNodeData
    {
        public bool isActive = false;
        public Quaternion rotation;
        public Vector3 accelerations;
    }

    public class AxisHubData
    {
        public Quaternion rotation;
        public Vector3 absolutePosition;
        internal bool isActive;
    }

    public class AxisOutputData
    {
        public const int NodesCount = 16;
        public List<AxisNodeData> nodesDataList;
        public AxisHubData hubData;
        public bool isActive = false;

        public AxisOutputData()
        {
            hubData = new AxisHubData();
            nodesDataList = new List<AxisNodeData>();
        }
    }
}
