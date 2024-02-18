using AxSlime.Axis;

static void OnTrackerData(object? sender, AxisOutputData data)
{
    Console.WriteLine(
        $"Axis Data: {{hubData: {{{data.hubData}}}, nodesData: [{string.Join(", ", data.nodesData.Select(n => $"{{{n}}}"))}]}}"
    );
}

try
{
    using var axisSocket = new AxisUdpSocket();
    axisSocket.OnAxisData += OnTrackerData;
    axisSocket.Start();

    Console.WriteLine("Axis receiver is running, press any key to exit.");
    Console.ReadKey();
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
