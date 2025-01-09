namespace jKnepel.ProteusNet.Networking.Packets
{
    internal enum EPacketType : byte
    {   
        ConnectionChallenge = 1,
        ChallengeAnswer = 2,
        ServerUpdate = 3,
        ClientUpdate = 4,
        Data = 5,
        SpawnObject = 6,
        UpdateObject = 7,
        DespawnObject = 8,
        DistributedAuthority = 9,
        Transform = 10
    }
}