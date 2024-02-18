using System.Drawing;

namespace Axis.Communication.Commander
{
    public enum CommandType
    {
        MainHeader,
        ImuZero,
        Buzz,
        LedColor,
        ResetIMU,
        SetMode,
        Calibration,
        StartStreaming,
        StopStreaming,
        SetStreamingMode,
        SinglePoseCalibration
    }

    public class AxisRuntimeCommander(AxisRuntimeUdpSocket axisUdpSocket)
    {
        private static readonly Dictionary<CommandType, byte[]> _cmdBytes =
            new()
            {
                { CommandType.MainHeader, [0x00, 0xEF, 0xAC, 0xEF, 0xAC] },
                { CommandType.ImuZero, [0x16, 0x02, 0x01] },
                { CommandType.Buzz, [0x80] },
                { CommandType.LedColor, [0x81] },
                { CommandType.SetMode, [0x21] },
                { CommandType.Calibration, [0x16, 0x01, 0x00] },
                { CommandType.StartStreaming, [0x01, 0xE0] },
                { CommandType.StopStreaming, [0x01, 0xE1] },
                { CommandType.SetStreamingMode, [0x01, 0xE2, 0x01, 0x00] },
                { CommandType.SinglePoseCalibration, [0x16, 0x00, 0x01] }
            };

        // Optimize as an array, keeping the dictionary for readability
        private static readonly byte[][] _cmdBytesArr = Enum.GetValues<CommandType>()
            .Order()
            .Select(c => _cmdBytes[c])
            .ToArray();

        private readonly AxisRuntimeUdpSocket _axisUdpSocket = axisUdpSocket;

        private static byte GetByteFromNormalizedFloat(float normalizedFloat)
        {
            return (byte)MathF.Round(Math.Clamp(normalizedFloat * 255f, 0f, 255f));
        }

        private static byte[] ToBytes(CommandType command)
        {
            return _cmdBytesArr[(int)command];
        }

        private void SendCommand(CommandType command)
        {
            _axisUdpSocket.SendData(ToBytes(command));
        }

        private void SendCommandWithHeader(CommandType command)
        {
            _axisUdpSocket.SendData([.. ToBytes(CommandType.MainHeader), .. ToBytes(command)]);
        }

        private void StartStreaming()
        {
            SendCommand(CommandType.StartStreaming);
            SendCommand(CommandType.SetStreamingMode);
        }

        private void StopStream()
        {
            SendCommand(CommandType.StopStreaming);
        }

        private void Reboot()
        {
            SendCommandWithHeader(CommandType.Calibration);
        }

        private void ZeroAll()
        {
            SendCommandWithHeader(CommandType.ImuZero);
        }

        private void SinglePoseCalibration()
        {
            SendCommandWithHeader(CommandType.SinglePoseCalibration);
        }

        private void SetNodeVibration(byte nodeIndex, float intensity, float durationSeconds)
        {
            _axisUdpSocket.SendData(
                [
                    .. ToBytes(CommandType.MainHeader),
                    .. ToBytes(CommandType.Buzz),
                    nodeIndex,
                    GetByteFromNormalizedFloat(intensity),
                    GetByteFromNormalizedFloat(durationSeconds / 25.5f)
                ]
            );
        }

        private void SetNodeLedColor(byte nodeIndex, Color color, float brightness)
        {
            brightness = brightness > 1f ? 1f / 3f : brightness / 3f;

            _axisUdpSocket.SendData(
                [
                    .. ToBytes(CommandType.MainHeader),
                    .. ToBytes(CommandType.LedColor),
                    nodeIndex,
                    color.R,
                    color.B,
                    color.G,
                    GetByteFromNormalizedFloat(brightness)
                ]
            );
        }
    }
}
