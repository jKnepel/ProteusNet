namespace jKnepel.ProteusNet.Networking.Packets
{
    internal enum EPacketType : byte
    {   
        ConnectionChallenge = 1,
        ChallengeAnswer = 2,
        ServerUpdate = 3,
        ClientUpdate = 4,
        Data = 5,
    }
}