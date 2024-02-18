using AxSlime.Axis;
using AxSlime.Slime;

static void OnTrackerData(object? sender, AxisOutputData data)
{
    Console.WriteLine(
        $"AXIS Data: {{hubData: {{{data.hubData}}}, nodesData: [{string.Join(", ", data.nodesData.Select(n => $"{{{n}}}"))}]}}"
    );
}

try
{
    using var axisSocket = new AxisUdpSocket();
    axisSocket.OnAxisData += OnTrackerData;
    axisSocket.Start();

    using var slimeSocket = new SlimeUdpSocket();
    slimeSocket.Start();
    slimeSocket.SendPacket(new Packet3Handshake() { MacAddress = PacketUtils.DefaultMacAddress });
    slimeSocket.SendPacket(new Packet15SensorInfo());

    using var slimeSocket2 = new SlimeUdpSocket();
    slimeSocket2.Start();
    slimeSocket2.SendPacket(
        new Packet3Handshake()
        {
            MacAddress = PacketUtils.IncrementedMacAddress(PacketUtils.DefaultMacAddress, 1)
        }
    );
    slimeSocket2.SendPacket(new Packet15SensorInfo());

    Console.WriteLine("AXIS receiver is running, press any key to stop the receiver.");
    Console.ReadKey();
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
