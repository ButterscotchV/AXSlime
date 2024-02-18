using System.Buffers.Binary;

namespace AxSlime.Slime
{
    public abstract class SlimePacket
    {
        public byte PacketId { get; }

        protected SlimePacket(byte packetId)
        {
            PacketId = packetId;
        }

        protected SlimePacket(SlimeTxPacketType packetType)
            : this((byte)packetType) { }

        protected SlimePacket(SlimeRxPacketType packetType)
            : this((byte)packetType) { }

        public abstract int Serialize(Span<byte> buffer);
        public abstract SlimePacket Deserialize(ReadOnlySpan<byte> data);

        public int SerializePacketId(Span<byte> buffer)
        {
            return SerializePacketId(buffer, PacketId);
        }

        public static int SerializePacketId(Span<byte> buffer, uint packetId)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, packetId);
            return sizeof(uint);
        }

        public static uint DeserializePacketId(ReadOnlySpan<byte> data)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(data);
        }
    }

    public abstract class SlimeSensorPacket : SlimePacket
    {
        public byte SensorId { get; set; }

        protected SlimeSensorPacket(byte packetId)
            : base(packetId) { }

        protected SlimeSensorPacket(SlimeTxPacketType packetType)
            : base(packetType) { }

        protected SlimeSensorPacket(SlimeRxPacketType packetType)
            : base(packetType) { }

        public override int Serialize(Span<byte> buffer)
        {
            buffer[0] = SensorId;
            return 1;
        }

        public override SlimeSensorPacket Deserialize(ReadOnlySpan<byte> buffer)
        {
            SensorId = buffer[0];
            return this;
        }
    }
}
