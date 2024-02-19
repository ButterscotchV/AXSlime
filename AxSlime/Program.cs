using AxSlime.Axis;
using AxSlime.Bridge;

BridgeController? bridge = null;
void OnTrackerData(object? sender, AxisOutputData data)
{
    Console.WriteLine($"AXIS Data: {data}");
    bridge ??= new BridgeController(data);
    bridge.Update();
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
finally
{
    bridge?.Dispose();
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
