using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct DataPacket
	{
		public enum DataPacketType : byte
		{
			ToClient,
			ToClients,
			Forwarded,
			ToServer
		}
		
		public static byte PacketType => (byte)EPacketType.Data;
		public DataPacketType DataType;
		public uint? SenderID;
		public uint? TargetID;
		public uint[] TargetIDs;
		
		public bool IsStructData;
		public uint DataID;
		public byte[] Data;

		public DataPacket(DataPacketType dataType, uint id, bool isStructData, uint dataID, byte[] data)
		{
			DataType = dataType;
			if (dataType == DataPacketType.Forwarded)
			{
				SenderID = id;
				TargetID = null;
				TargetIDs = null;
			}
			else if (dataType == DataPacketType.ToClient)
			{
				SenderID = null;
				TargetID = id;
				TargetIDs = null;
			}
			else 
			{
				throw new("The constructed data packet is not valid!");
			}
			
			IsStructData = isStructData;
			DataID = dataID;
			Data = data;
		}
		
		public DataPacket(uint[] targetIDs, bool isStructData, uint dataID, byte[] data)
		{
			DataType = DataPacketType.ToClients;
			SenderID = null;
			TargetID = null;
			TargetIDs = targetIDs;
			
			IsStructData = isStructData;
			DataID = dataID;
			Data = data;
		}

		public DataPacket(bool isStructData, uint dataID, byte[] data)
		{
			DataType = DataPacketType.ToServer;
			SenderID = null;
			TargetID = null;
			TargetIDs = null;
			
			IsStructData = isStructData;
			DataID = dataID;
			Data = data;
		}

		public static DataPacket Read(Reader reader)
		{
			var dataType = (DataPacketType)reader.ReadByte();
			switch (dataType)
			{
				case DataPacketType.ToClients:
				{
					var targetIDs = reader.ReadArray<uint>();
					var isStructData = reader.ReadBoolean();
					var dataID = reader.ReadUInt32();
					var simpleData = reader.ReadRemainingBuffer();
					return new(targetIDs, isStructData, dataID, simpleData);
				}
				case DataPacketType.ToServer:
				{
					var isStructData = reader.ReadBoolean();
					var dataID = reader.ReadUInt32();
					var simpleData = reader.ReadRemainingBuffer();
					return new(isStructData, dataID, simpleData);
				}
				default:
				{
					var id = reader.ReadUInt32();
					var isStructData = reader.ReadBoolean();
					var dataID = reader.ReadUInt32();
					var simpleData = reader.ReadRemainingBuffer();
					return new(dataType, id, isStructData, dataID, simpleData);
				}
			}
		}

		public static void Write(Writer writer, DataPacket packet)
		{
			writer.WriteByte((byte)packet.DataType);
			switch (packet.DataType)
			{
				case DataPacketType.Forwarded:
					// ReSharper disable once PossibleInvalidOperationException
					writer.WriteUInt32((uint)packet.SenderID);
					break;
				case DataPacketType.ToClient:
					// ReSharper disable once PossibleInvalidOperationException
					writer.WriteUInt32((uint)packet.TargetID);
					break;
				case DataPacketType.ToClients:
					writer.WriteArray(packet.TargetIDs);
					break;
			}
			
			writer.WriteBoolean(packet.IsStructData);
			writer.WriteUInt32(packet.DataID);
			writer.BlockCopy(ref packet.Data, 0, packet.Data.Length);
		}
	}
}
