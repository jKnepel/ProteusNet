using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct UpdateObjectPacket
	{
		public static byte PacketType => (byte)EPacketType.UpdateObject;
		public readonly uint ObjectIdentifier;
		public readonly uint? ObjectParentIdentifier;
		public readonly bool IsActive;

		public UpdateObjectPacket(uint objectIdentifier, uint? objectParentIdentifier, bool isActive = true)
		{
			ObjectIdentifier = objectIdentifier;
			ObjectParentIdentifier = objectParentIdentifier;
			IsActive = isActive;
		}

		public static UpdateObjectPacket Read(Reader reader)
		{
			var networkObjectIdentifier = reader.ReadUInt32();
			uint? parentIdentifier = null;
			if (reader.ReadBoolean())
				parentIdentifier = reader.ReadUInt32();
			var activeSelf = reader.ReadBoolean();
			return new(networkObjectIdentifier, parentIdentifier, activeSelf);
		}

		public static void Write(Writer writer, UpdateObjectPacket packet)
		{
			writer.WriteUInt32(packet.ObjectIdentifier);
			var hasParent = packet.ObjectParentIdentifier != null;
			writer.WriteBoolean(hasParent);
			if (hasParent)
				writer.WriteUInt32((uint)packet.ObjectParentIdentifier);
			writer.WriteBoolean(packet.IsActive);
		}
	}
}
