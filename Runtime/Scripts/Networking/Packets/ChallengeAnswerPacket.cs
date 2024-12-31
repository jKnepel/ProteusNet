using jKnepel.ProteusNet.Serializing;
using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Packets
{
	internal struct ChallengeAnswerPacket
	{
		public static byte PacketType => (byte)EPacketType.ChallengeAnswer;
		public byte[] ChallengeAnswer;
		public readonly string Username;
		public Color32 Colour;

		public const int CHALLENGE_ANSWER_LENGTH = 32;

		public ChallengeAnswerPacket(byte[] challengeAnswer, string username, Color32 colour)
		{
			ChallengeAnswer = challengeAnswer;
			Username = username;
			Colour = colour;
		}

		public static ChallengeAnswerPacket Read(Reader reader)
		{
			var challengeAnswer = reader.ReadByteArray(CHALLENGE_ANSWER_LENGTH);
			var username = reader.ReadString();
			var colour = reader.ReadColor32WithoutAlpha();
			return new(challengeAnswer, username, colour);
		}

		public static void Write(Writer writer, ChallengeAnswerPacket packet)
		{
			writer.BlockCopy(ref packet.ChallengeAnswer, 0, CHALLENGE_ANSWER_LENGTH);
			writer.WriteString(packet.Username);
			writer.WriteColor32WithoutAlpha(packet.Colour);
		}
	}
}
