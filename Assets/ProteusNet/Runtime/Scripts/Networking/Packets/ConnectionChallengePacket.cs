using jKnepel.ProteusNet.Serializing;

namespace jKnepel.ProteusNet.Networking.Packets
{
    internal struct ConnectionChallengePacket
    {
        public static byte PacketType => (byte)EPacketType.ConnectionChallenge;
        public ulong Challenge;

        public ConnectionChallengePacket(ulong challenge)
		{
            Challenge = challenge;
		}

        public static ConnectionChallengePacket Read(Reader reader)
		{
            ulong challenge = reader.ReadUInt64();
            return new(challenge);
		}

        public static void Write(Writer writer, ConnectionChallengePacket packet)
		{
            writer.WriteUInt64(packet.Challenge);
		}
    }
}
