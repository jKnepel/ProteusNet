using jKnepel.ProteusNet.Serializing;
using System;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal class UpdateObjectPacket
	{
		[Flags]
		public enum EFlags
		{
			Nothing = 0,
			Active = 1,
			Parent = 2,
			Authority = 4
		}
		
		public static byte PacketType => (byte)EPacketType.UpdateObject;
		public uint ObjectIdentifier { get; }
		public EFlags Flags { get; private set; }
		
		public bool IsActive { get; private set; }
		public uint? ParentIdentifier { get; private set; }
		public uint AuthorID { get; private set; }
		public ushort AuthoritySequence { get; private set; }
		public uint OwnerID { get; private set; }
		public ushort OwnershipSequence { get; private set; }

		private UpdateObjectPacket(uint objectIdentifier)
		{
			ObjectIdentifier = objectIdentifier;
		}

		public static UpdateObjectPacket Read(Reader reader)
		{
			var packet = new UpdateObjectPacket(reader.ReadUInt32());
			packet.Flags = (EFlags)reader.ReadByte();
			
			if (packet.Flags.HasFlag(EFlags.Active))
			{
				packet.IsActive = reader.ReadBoolean();
			}
			if (packet.Flags.HasFlag(EFlags.Parent))
			{
				if (reader.ReadBoolean())
					packet.ParentIdentifier = reader.ReadUInt32();
			}
			if (packet.Flags.HasFlag(EFlags.Authority))
			{
				packet.AuthorID = reader.ReadUInt32();
				packet.AuthoritySequence = reader.ReadUInt16();
				packet.OwnerID = reader.ReadUInt32();
				packet.OwnershipSequence = reader.ReadUInt16();
			}
			return packet;
		}

		public static void Write(Writer writer, UpdateObjectPacket packet)
		{
			writer.WriteUInt32(packet.ObjectIdentifier);
			writer.WriteByte((byte)packet.Flags);
			
			if (packet.Flags.HasFlag(EFlags.Active))
			{
				writer.WriteBoolean(packet.IsActive);
			}
			if (packet.Flags.HasFlag(EFlags.Parent))
			{
				writer.WriteBoolean(packet.ParentIdentifier != null);
				if (packet.ParentIdentifier != null)
					writer.WriteUInt32((uint)packet.ParentIdentifier);
			}
			if (packet.Flags.HasFlag(EFlags.Authority))
			{
				writer.WriteUInt32(packet.AuthorID);
				writer.WriteUInt16(packet.AuthoritySequence);
				writer.WriteUInt32(packet.OwnerID);
				writer.WriteUInt16(packet.OwnershipSequence);
			}
		}

		public class Builder
		{
			private UpdateObjectPacket _packet;
			private readonly uint _objectIdentifier;
			
			public Builder(uint objectIdentifier)
			{
				_objectIdentifier = objectIdentifier;
				_packet = new(objectIdentifier);
			}

			public Builder WithActiveUpdate(bool isActive)
			{
				_packet.IsActive = isActive;
				_packet.Flags |= EFlags.Active;
				return this;
			}

			public Builder WithParentUpdate(uint? parentIdentifier)
			{
				_packet.ParentIdentifier = parentIdentifier;
				_packet.Flags |= EFlags.Parent;
				return this;
			}

			public Builder WithAuthorityUpdate(uint authorID, ushort authoritySequence, uint ownerID, ushort ownershipSequence)
			{
				_packet.AuthorID = authorID;
				_packet.AuthoritySequence = authoritySequence;
				_packet.OwnerID = ownerID;
				_packet.OwnershipSequence = ownershipSequence;
				_packet.Flags |= EFlags.Authority;
				return this;
			}

			public UpdateObjectPacket Build() => _packet;
			public void Reset() => _packet = new(_objectIdentifier);
		}
	}
}
