using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct SpawnObjectPacket
	{
		public enum EObjectType : byte
		{
			Placed,
			Instantiated
		}
		
		public static byte PacketType => (byte)EPacketType.SpawnObject;
		public readonly EObjectType ObjectType;
		public readonly uint ObjectIdentifier;
		public readonly uint? ObjectParentIdentifier;
		public readonly uint? PrefabIdentifier;

		public SpawnObjectPacket(uint objectIdentifier, uint? objectParentIdentifier)
		{
			ObjectType = EObjectType.Placed;
			ObjectIdentifier = objectIdentifier;
			ObjectParentIdentifier = objectParentIdentifier;
			PrefabIdentifier = null;
		}

		public SpawnObjectPacket(uint objectIdentifier, uint? objectParentIdentifier, uint prefabIdentifier)
		{
			ObjectType = EObjectType.Instantiated;
			ObjectIdentifier = objectIdentifier;
			ObjectParentIdentifier = objectParentIdentifier;
			PrefabIdentifier = prefabIdentifier;
		}

		public static SpawnObjectPacket Read(Reader reader)
		{
			var objectType = (EObjectType)reader.ReadByte();
			var networkObjectIdentifier = reader.ReadUInt32();
			uint? parentIdentifier = null;
			if (reader.ReadBoolean())
				parentIdentifier = reader.ReadUInt32();
			
			switch (objectType)
			{
				case EObjectType.Placed:
					return new(networkObjectIdentifier, parentIdentifier);
				case EObjectType.Instantiated:
					var prefabIdentifier = reader.ReadUInt32();
					return new(networkObjectIdentifier, parentIdentifier, prefabIdentifier);
				default: throw new();
			}
		}

		public static void Write(Writer writer, SpawnObjectPacket packet)
		{
			writer.WriteByte((byte)packet.ObjectType);
			writer.WriteUInt32(packet.ObjectIdentifier);
			var hasParent = packet.ObjectParentIdentifier != null;
			writer.WriteBoolean(hasParent);
			if (hasParent)
				writer.WriteUInt32((uint)packet.ObjectParentIdentifier);

			if (packet.ObjectType == EObjectType.Instantiated)
			{
				// ReSharper disable once PossibleInvalidOperationException
				writer.WriteUInt32((uint)packet.PrefabIdentifier);
			}
		}
	}
}
