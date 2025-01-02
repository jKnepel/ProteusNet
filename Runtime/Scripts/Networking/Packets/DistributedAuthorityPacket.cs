using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
    internal struct DistributedAuthorityPacket
    {
        public enum EType : byte
        {
            RequestAuthority,
            ReleaseAuthority,
            RequestOwnership,
            ReleaseOwnership
        }
        
        public static byte PacketType => (byte)EPacketType.DistributedAuthority;
        public readonly uint ObjectIdentifier;
        public readonly EType Type;
        public readonly ushort AuthoritySequence;
        public readonly ushort OwnershipSequence;

        public DistributedAuthorityPacket(uint objectIdentifier, EType type, ushort authoritySequence, ushort ownershipSequence)
        {
            ObjectIdentifier = objectIdentifier;
            Type = type;
            AuthoritySequence = authoritySequence;
            OwnershipSequence = ownershipSequence;
        }

        public static DistributedAuthorityPacket Read(Reader reader)
        {
            var objectIdentifier = reader.ReadUInt32();
            var type = reader.ReadByte();
            var authority = reader.ReadUInt16();
            var ownership = reader.ReadUInt16();
            return new(objectIdentifier, (EType)type, authority, ownership);
        }

        public static void Write(Writer writer, DistributedAuthorityPacket packet)
        {
            writer.WriteUInt32(packet.ObjectIdentifier);
            writer.WriteByte((byte)packet.Type);
            writer.WriteUInt16(packet.AuthoritySequence);
            writer.WriteUInt16(packet.OwnershipSequence);
        }
    }
}
