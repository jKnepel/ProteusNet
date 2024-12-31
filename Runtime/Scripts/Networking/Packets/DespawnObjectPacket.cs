using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct DespawnObjectPacket
	{
		public static byte PacketType => (byte)EPacketType.DespawnObject;
		public readonly uint ObjectIdentifier;

		public DespawnObjectPacket(uint objectIdentifier)
		{
			ObjectIdentifier = objectIdentifier;
		}

		public static DespawnObjectPacket Read(Reader reader)
		{
			var objectIdentifier = reader.ReadUInt32();
			return new(objectIdentifier);
		}

		public static void Write(Writer writer, DespawnObjectPacket packet)
		{
			writer.WriteUInt32(packet.ObjectIdentifier);
		}
	}
}
