namespace AxSlime.Slime
{
    public enum SlimeTxPacketType : byte
    {
        Heartbeat = 0,
        Handshake = 3,
        Accel = 4,
        Config = 8,
        PingPong = 10,
        Serial = 11,
        BatteryLevel = 12,
        Tap = 13,
        Error = 14,
        SensorInfo = 15,
        RotationData = 17,
        MagnetometerAccuracy = 18,
        SignalStrength = 19,
        Temperature = 20,
        UserAction = 21,
        FeatureFlags = 22,
        PacketBundle = 100,
        Inspection = 105,
    }

    public enum SlimeRxPacketType : byte
    {
        Heartbeat = 1,
        Vibrate = 2,
        Handshake = 3,
        Command = 4,
    }
}
