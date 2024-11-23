using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct SpawnObjectPacket
	{
		public static byte PacketType => (byte)EPacketType.SpawnObject;
		public readonly uint ObjectIdentifier;
		public readonly uint? ObjectParentIdentifier;

		public SpawnObjectPacket(uint objectIdentifier, uint? objectParentIdentifier)
		{
			ObjectIdentifier = objectIdentifier;
			ObjectParentIdentifier = objectParentIdentifier;
		}

		public static SpawnObjectPacket Read(Reader reader)
		{
			var networkObjectIdentifier = reader.ReadUInt32();
			uint? parentIdentifier = null;
			if (reader.ReadBoolean())
				parentIdentifier = reader.ReadUInt32();
			return new(networkObjectIdentifier, parentIdentifier);
		}

		public static void Write(Writer writer, SpawnObjectPacket packet)
		{
			writer.WriteUInt32(packet.ObjectIdentifier);
			var hasParent = packet.ObjectParentIdentifier != null;
			writer.WriteBoolean(hasParent);
			if (hasParent)
				writer.WriteUInt32((uint)packet.ObjectParentIdentifier);
		}
	}
}
