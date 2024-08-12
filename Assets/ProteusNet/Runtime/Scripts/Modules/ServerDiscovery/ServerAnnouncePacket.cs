using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    internal struct ServerAnnouncePacket
    {
        public ushort Port;
        public string Servername;
        public uint MaxNumberOfClients;
        public uint NumberOfClients;

        public ServerAnnouncePacket(ushort port, string servername, uint maxNumberOfClients, uint numberOfClients)
        {
            Port = port;
            Servername = servername;
            MaxNumberOfClients = maxNumberOfClients;
            NumberOfClients = numberOfClients;
        }

        public static ServerAnnouncePacket Read(Reader reader)
        {
            var port = reader.ReadUInt16();
            var servername = reader.ReadString();
            var maxNumberOfClients = reader.ReadUInt32();
            var numberOfClients = reader.ReadUInt32();
            return new(port, servername, maxNumberOfClients, numberOfClients);
        }

        public static void Write(Writer writer, ServerAnnouncePacket packet)
        {
            writer.WriteUInt16(packet.Port);
            writer.WriteString(packet.Servername);
            writer.WriteUInt32(packet.MaxNumberOfClients);
            writer.WriteUInt32(packet.NumberOfClients);
        }
    }
}
