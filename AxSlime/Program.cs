using AxSlime.Axis;

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

    Console.WriteLine("AXIS receiver is running, press any key to stop the receiver.");
    Console.ReadKey();
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
