using jKnepel.ProteusNet.Serialising;
using System;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct ServerUpdatePacket
	{
		public enum UpdateType : byte
		{
			Authenticated,
			Updated
		}
		
		public static byte PacketType => (byte)EPacketType.ServerUpdate;
		public readonly UpdateType Type;
		public readonly uint? ClientID;
		public readonly string Servername;
		public readonly uint? MaxNumberConnectedClients;

		public ServerUpdatePacket(uint clientID, string servername, uint maxNumberConnectedClients)
		{
			Type = UpdateType.Authenticated;
			ClientID = clientID;
			Servername = servername;
			MaxNumberConnectedClients = maxNumberConnectedClients;
		}

		public ServerUpdatePacket(string servername)
		{
			Type = UpdateType.Updated;
			ClientID = null;
			Servername = servername;
			MaxNumberConnectedClients = null;
		}

		public static ServerUpdatePacket Read(Reader reader)
		{
			var type = (UpdateType)reader.ReadByte();
			switch (type)
			{
				case UpdateType.Authenticated:
				{
					var clientID = reader.ReadUInt32();
					var servername = reader.ReadString();
					var maxNumberConnectedClients = reader.ReadUInt32();
					return new(clientID, servername, maxNumberConnectedClients);
				}
				case UpdateType.Updated:
				{
					var servername = reader.ReadString();
					return new(servername);
				}
				default:
					throw new ArgumentException("Invalid server update packet type received");
			}
		}

		public static void Write(Writer writer, ServerUpdatePacket packet)
		{
			writer.WriteByte((byte)packet.Type);
			switch (packet.Type)
			{
				case UpdateType.Authenticated:
					if (packet.ClientID is null || packet.Servername is null || packet.MaxNumberConnectedClients is null)
						throw new NullReferenceException("Invalid server update packet values supplied");
					writer.WriteUInt32((uint)packet.ClientID);
					writer.WriteString(packet.Servername);
					writer.WriteUInt32((uint)packet.MaxNumberConnectedClients);
					break;
				case UpdateType.Updated:
					writer.WriteString(packet.Servername);
					break;
				default:
					throw new ArgumentException("Invalid server update packet type supplied");
			}
			
		}
	}
}
