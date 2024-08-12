using jKnepel.ProteusNet.Serializing;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct ClientUpdatePacket
	{
		public enum UpdateType : byte
		{
			Connected,
			Disconnected,
			Updated
		}
		
		public static byte PacketType => (byte)EPacketType.ClientUpdate;
		public readonly uint ClientID;
		public readonly UpdateType Type;
		public readonly string Username;
		public readonly Color32? Colour;

		public ClientUpdatePacket(uint clientID, UpdateType type, string username, Color32? colour)
		{
			ClientID = clientID;
			Type = type;
			Username = username;
			Colour = colour;
		}

		public ClientUpdatePacket(uint clientID)
		{
			ClientID = clientID;
			Type = UpdateType.Disconnected;
			Username = null;
			Colour = default;
		}

		public static ClientUpdatePacket Read(Reader reader)
		{
			var clientID = reader.ReadUInt32();
			var type = (UpdateType)reader.ReadByte();
			
			var hasUsername = reader.ReadBoolean();
			string username = null;
			if (hasUsername)
				username = reader.ReadString();
			var hasColour = reader.ReadBoolean();
			Color32? colour = null;
			if (hasColour)
				colour = reader.ReadColor32WithoutAlpha();
			
			return new(clientID, type, username, colour);
		}

		public static void Write(Writer writer, ClientUpdatePacket packet)
		{
			writer.WriteUInt32(packet.ClientID);
			writer.WriteByte((byte)packet.Type);
			
			writer.WriteBoolean(packet.Username is not null);
			if (packet.Username is not null)
				writer.WriteString(packet.Username);
			writer.WriteBoolean(packet.Colour is not null);
			if (packet.Colour is not null)
				writer.WriteColor32WithoutAlpha((Color32)packet.Colour);
		}
	}
}
