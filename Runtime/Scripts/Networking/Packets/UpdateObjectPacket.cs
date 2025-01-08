using jKnepel.ProteusNet.Serializing;
using System;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal class UpdateObjectPacket
	{
		[Flags]
		public enum EFlags
		{
			Nothing = 0,
			Parent = 1,
			Active = 2,
			Authority = 4
		}
		
		public static byte PacketType => (byte)EPacketType.UpdateObject;
		public uint ObjectIdentifier { get; }
		public EFlags Flags { get; private set; }
		
		public uint? ParentIdentifier { get; private set; }
		public bool? IsActive { get; private set; }
		
		public uint? AuthorID { get; private set; }
		public ushort? AuthoritySequence { get; private set; }
		public uint? OwnerID { get; private set; }
		public ushort? OwnershipSequence { get; private set; }

		private UpdateObjectPacket(uint objectIdentifier)
		{
			ObjectIdentifier = objectIdentifier;
		}

		public static UpdateObjectPacket Read(Reader reader)
		{
			var packet = new UpdateObjectPacket(reader.ReadUInt32());
			packet.Flags = (EFlags)reader.ReadByte();
			if (packet.Flags.HasFlag(EFlags.Parent))
			{
				if (reader.ReadBoolean())
					packet.ParentIdentifier = reader.ReadUInt32();
			}
			if (packet.Flags.HasFlag(EFlags.Active))
			{
				packet.IsActive = reader.ReadBoolean();
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
			if (packet.Flags.HasFlag(EFlags.Parent))
			{
				writer.WriteBoolean(packet.ParentIdentifier != null);
				if (packet.ParentIdentifier != null)
					writer.WriteUInt32((uint)packet.ParentIdentifier);
			}
			if (packet.Flags.HasFlag(EFlags.Active))
			{
				Debug.Assert(packet.IsActive != null, "IsActive is null and included in the flags");
				writer.WriteBoolean((bool)packet.IsActive);
			}
			if (packet.Flags.HasFlag(EFlags.Authority))
			{
				Debug.Assert(packet is { AuthorID: not null, AuthoritySequence: not null, OwnerID: not null, OwnershipSequence: not null }, "Authority is null and included in the flags");
				writer.WriteUInt32((uint)packet.AuthorID);
				writer.WriteUInt16((ushort)packet.AuthoritySequence);
				writer.WriteUInt32((uint)packet.OwnerID);
				writer.WriteUInt16((ushort)packet.OwnershipSequence);
			}
		}

		public class Builder
		{
			private readonly UpdateObjectPacket _packet;
			
			public Builder(uint objectIdentifier)
			{
				_packet = new(objectIdentifier);
			}

			public Builder WithParentUpdate(uint? parentIdentifier)
			{
				_packet.ParentIdentifier = parentIdentifier;
				_packet.Flags |= EFlags.Parent;
				return this;
			}

			public Builder WithActiveUpdate(bool isActive)
			{
				_packet.IsActive = isActive;
				_packet.Flags |= EFlags.Active;
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
		}
	}
}
