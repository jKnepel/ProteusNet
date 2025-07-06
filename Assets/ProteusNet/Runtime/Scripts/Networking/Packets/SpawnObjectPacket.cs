using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Serializing;
using System;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal class SpawnObjectPacket
	{
		[Flags]
		public enum EFlags : byte
		{
			Placed = 1,
			Instantiated = 2,
			HasParent = 4,
			HasAuthor = 8,
			HasAuthorSequence = 16,
			HasOwner = 32,
			HasOwnerSequence = 64
		}
		
		public static byte PacketType => (byte)EPacketType.SpawnObject;
		public uint ObjectIdentifier { get; }
		public EFlags Flags { get; private set; }
		
		public uint PrefabIdentifier { get; private set; }
		public bool IsActive { get; private set; }
		public uint ParentIdentifier { get; private set; }
		
		public uint AuthorID { get; private set; }
		public ushort AuthorSequence { get; private set; }
		public uint OwnerID { get; private set; }
		public ushort OwnerSequence { get; private set; }

		private SpawnObjectPacket(uint objectIdentifier, EFlags flags)
		{
			ObjectIdentifier = objectIdentifier;
			Flags = flags;
		}

		public static SpawnObjectPacket Read(Reader reader)
		{
			var objectIdentifier = reader.ReadUInt32();
			var flags = (EFlags)reader.ReadByte();
			var packet = new SpawnObjectPacket(objectIdentifier, flags);

			if (flags.HasFlag(EFlags.Instantiated))
				packet.PrefabIdentifier = reader.ReadUInt32();

			packet.IsActive = reader.ReadBoolean();

			if (flags.HasFlag(EFlags.HasParent))
				packet.ParentIdentifier = reader.ReadUInt32();

			if (flags.HasFlag(EFlags.HasAuthor))
				packet.AuthorID = reader.ReadUInt32();
			if (flags.HasFlag(EFlags.HasAuthorSequence))
				packet.AuthorSequence = reader.ReadUInt16();
			if (flags.HasFlag(EFlags.HasOwner))
				packet.OwnerID = reader.ReadUInt32();
			if (flags.HasFlag(EFlags.HasOwnerSequence))
				packet.OwnerSequence = reader.ReadUInt16();

			return packet;
		}

		public static void Write(Writer writer, SpawnObjectPacket packet)
		{
			writer.WriteUInt32(packet.ObjectIdentifier);
			writer.WriteByte((byte)packet.Flags);

			if (packet.Flags.HasFlag(EFlags.Instantiated))
				writer.WriteUInt32(packet.PrefabIdentifier);

			writer.WriteBoolean(packet.IsActive);

			if (packet.Flags.HasFlag(EFlags.HasParent))
				writer.WriteUInt32(packet.ParentIdentifier);
			
			if (packet.Flags.HasFlag(EFlags.HasAuthor))
				writer.WriteUInt32(packet.AuthorID);
			if (packet.Flags.HasFlag(EFlags.HasAuthorSequence))
				writer.WriteUInt16(packet.AuthorSequence);
			if (packet.Flags.HasFlag(EFlags.HasOwner))
				writer.WriteUInt32(packet.OwnerID);
			if (packet.Flags.HasFlag(EFlags.HasOwnerSequence))
				writer.WriteUInt16(packet.OwnerSequence);
		}

		public static SpawnObjectPacket Build(int prefabIdentifier, NetworkObject networkObject)
		{
			var flags = networkObject.ObjectType == EObjectType.Placed ? EFlags.Placed : EFlags.Instantiated;
			var packet = new SpawnObjectPacket(networkObject.ObjectIdentifier, flags);

			if (networkObject.ObjectType == EObjectType.Instantiated)
				packet.PrefabIdentifier = (uint)prefabIdentifier;

			packet.IsActive = networkObject.gameObject.activeSelf;

			if (networkObject.ParentIdentifier != null)
			{
				packet.Flags |= EFlags.HasParent;
				packet.ParentIdentifier = (uint)networkObject.ParentIdentifier;
			}

			if (networkObject.AuthorID != 0)
			{
				packet.Flags |= EFlags.HasAuthor;
				packet.AuthorID = networkObject.AuthorID;
			}
			if (networkObject.AuthoritySequence != 0)
			{
				packet.Flags |= EFlags.HasAuthorSequence;
				packet.AuthorSequence = networkObject.AuthoritySequence;
			}
			if (networkObject.OwnerID != 0)
			{
				packet.Flags |= EFlags.HasOwner;
				packet.OwnerID = networkObject.OwnerID;
			}
			if (networkObject.OwnershipSequence != 0)
			{
				packet.Flags |= EFlags.HasOwnerSequence;
				packet.OwnerSequence = networkObject.OwnershipSequence;
			}
			
			return packet;
		}
	}
}
