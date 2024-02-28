using AxSlime;
using AxSlime.Axis;
using AxSlime.Bridge;
using AxSlime.Config;

AxSlimeConfig config;
BridgeController? bridge = null;
void OnTrackerData(object? sender, AxisOutputData data)
{
    Console.WriteLine($"AXIS Data: {data}");
    bridge ??= new BridgeController(data, config.SlimeVrEndPoint);
    bridge.Update();
}

try
{
    // Print program info
    Console.WriteLine($"Starting {Constants.Name} v{Constants.Version} ({Constants.Url})...\n");

    // Load config
    JsonConfigHandler<AxSlimeConfig> configHandler = new JsonConfigHandler<AxSlimeConfig>(
        "AXSlime_Config.json",
        JsonConfigHandler<AxSlimeConfig>.Context.AxSlimeConfig
    );
    config = configHandler.InitializeConfig(AxSlimeConfig.Default);

    // Fill in new values if the version changed
    if (config.ConfigVersion < AxSlimeConfig.Default.ConfigVersion)
    {
        var backupFile = $"{configHandler.ConfigFilePath}.old";
        configHandler.MakeBackup(backupFile);
        Console.WriteLine(
            $"Backed up the current config file to \"{backupFile}\", upgrading from v{config.ConfigVersion} to v{AxSlimeConfig.Default.ConfigVersion}..."
        );
        config.ConfigVersion = AxSlimeConfig.Default.ConfigVersion;
        configHandler.WriteConfig(config);
    }

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
