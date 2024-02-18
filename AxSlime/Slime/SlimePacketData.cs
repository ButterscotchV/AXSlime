using System.Buffers.Binary;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;

namespace AxSlime.Slime
{
    public enum PacketImuType : byte
    {
        BNO085 = 4,
    }

    public static class PacketUtils
    {
        public static int SerializeShortString(Span<byte> buffer, string str)
        {
            var strLen = Encoding.ASCII.GetBytes(str, buffer[1..]);
            buffer[0] = (byte)strLen;

            return strLen + sizeof(byte);
        }

        public static int SerializeBytes(Span<byte> buffer, ReadOnlySpan<byte> data)
        {
            data.CopyTo(buffer);
            return data.Length;
        }

        public static PhysicalAddress? GetMacAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                )
                .MaxBy(nic =>
                {
                    var stats = nic.GetIPStatistics();
                    return stats.BytesSent + stats.BytesReceived;
                })
                ?.GetPhysicalAddress();
        }

        public static readonly byte[] EmptyMacAddress = [0, 0, 0, 0, 0, 0];
        public static readonly byte[] MacAddress =
            GetMacAddress()?.GetAddressBytes() ?? EmptyMacAddress;

        public static byte[] IncrementedMacAddress(int amount)
        {
            if (amount <= 0)
                return MacAddress;

            var addr = GetMacAddress()?.GetAddressBytes();
            if (addr == null)
                return EmptyMacAddress;

            for (var i = 0; i < amount; i++)
            {
                Increment(addr, addr.Length - 1);
            }

            return addr;
        }

        private static bool Increment(byte[] data, int i)
        {
            if (i < 0)
                return false;

            if (data[i] < 255)
            {
                data[i]++;
            }
            else
            {
                if (!Increment(data, i - 1))
                    return false;
                data[i] = 0;
            }

            return true;
        }
    }

    public class Packet0Heartbeat : SlimePacket
    {
        public Packet0Heartbeat()
            : base(SlimeTxPacketType.Heartbeat) { }

        public override int Serialize(Span<byte> buffer)
        {
            return 0;
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            return 0;
        }
    }

    public class Packet3Handshake : SlimePacket
    {
        // 3 UInt32s of padding
        public static readonly byte[] Padding = new byte[sizeof(uint) * 3];

        public PacketBoardType BoardType { get; set; } = PacketBoardType.Wrangler;
        public PacketImuType ImuType { get; set; } = PacketImuType.BNO085;
        public PacketMcuType McuType { get; set; } = PacketMcuType.Wrangler;
        public uint FirmwareBuildNumber { get; set; } = 17;
        public string FirmwareVersion { get; set; } = "0.4.0";
        public int MacAddressOffset { get; set; } = 0;

        public Packet3Handshake()
            : base(SlimeTxPacketType.Handshake) { }

        public enum PacketBoardType : uint
        {
            Unknown = 0,
            Custom = 4,
            Wrangler = 14,
        }

        public enum PacketMcuType : uint
        {
            Unknown = 0,
            Wrangler = 4,
        }

        public override int Serialize(Span<byte> buffer)
        {
            var i = 0;

            BinaryPrimitives.WriteUInt32BigEndian(buffer[i..], (uint)BoardType);
            i += sizeof(uint);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[i..], (uint)ImuType);
            i += sizeof(uint);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[i..], (uint)McuType);
            i += sizeof(uint);
            Padding.CopyTo(buffer[i..]);
            i += Padding.Length;
            BinaryPrimitives.WriteUInt32BigEndian(buffer[i..], FirmwareBuildNumber);
            i += sizeof(uint);
            i += PacketUtils.SerializeShortString(buffer[i..], FirmwareVersion);
            i += PacketUtils.SerializeBytes(
                buffer[i..],
                PacketUtils.IncrementedMacAddress(MacAddressOffset)
            );

            return i;
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            return 0;
        }
    }

    public class Packet4Accel : SlimeSensorPacket
    {
        public Vector3 Acceleration { get; set; }

        public Packet4Accel()
            : base(SlimeTxPacketType.Accel) { }

        public override int Serialize(Span<byte> buffer)
        {
            var i = 0;

            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Acceleration.X);
            i += sizeof(float);
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Acceleration.Y);
            i += sizeof(float);
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Acceleration.Z);
            i += sizeof(float);
            i += base.Serialize(buffer);

            return i;
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            return 0;
        }
    }

    public class Packet10PingPong : SlimePacket
    {
        public int PingId { get; set; }

        public Packet10PingPong()
            : base(SlimeTxPacketType.PingPong) { }

        public override int Serialize(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32BigEndian(buffer, PingId);
            return sizeof(int);
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            PingId = BinaryPrimitives.ReadInt32BigEndian(data);
            return sizeof(int);
        }
    }

    public class Packet15SensorInfo : SlimeSensorPacket
    {
        public PacketSensorStatus SensorStatus { get; set; } = PacketSensorStatus.Ok;
        public PacketImuType SensorType { get; set; } = PacketImuType.BNO085;

        public Packet15SensorInfo()
            : base(SlimeTxPacketType.SensorInfo) { }

        public enum PacketSensorStatus : byte
        {
            Disconnected = 0,
            Ok = 1,
            Error = 2,
        }

        public override int Serialize(Span<byte> buffer)
        {
            var i = base.Serialize(buffer);

            buffer[i++] = (byte)SensorStatus;
            buffer[i++] = (byte)SensorType;

            return i;
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            return 0;
        }
    }

    public class Packet17RotationData : SlimeSensorPacket
    {
        public PacketDataType DataType { get; set; } = PacketDataType.Normal;
        public Quaternion Rotation { get; set; }
        public byte AccuracyInfo { get; set; } = 0;

        public Packet17RotationData()
            : base(SlimeTxPacketType.RotationData) { }

        public enum PacketDataType : byte
        {
            Normal = 1,
            Correction = 2,
        }

        public override int Serialize(Span<byte> buffer)
        {
            var i = base.Serialize(buffer);

            buffer[i++] = (byte)DataType;
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Rotation.X);
            i += sizeof(float);
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Rotation.Y);
            i += sizeof(float);
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Rotation.Z);
            i += sizeof(float);
            BinaryPrimitives.WriteSingleBigEndian(buffer[i..], Rotation.W);
            i += sizeof(float);
            buffer[i++] = AccuracyInfo;

            return i;
        }

        public override int Deserialize(ReadOnlySpan<byte> data)
        {
            return 0;
        }
    }
}
