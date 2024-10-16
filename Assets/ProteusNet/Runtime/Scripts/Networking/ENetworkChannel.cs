namespace jKnepel.ProteusNet.Networking
{
    public enum ENetworkChannel : byte
    {
        /// <summary>
        /// Will ensure the arrival of packets by resending if the packet does not arrive or is faulty.
        /// Will also buffer packets until all missing packets arrived, to handle them in order.
        /// </summary>
        ReliableOrdered,
        /// <summary>
        /// Will ensure the arrival of packets by resending if the packet does not arrive or is faulty.
        /// Will handle packets as they arrive.
        /// </summary>
        ReliableUnordered,
        /// <summary>
        /// Will handle packets as they arrive and drop any packets arriving out of order.
        /// </summary>
        UnreliableOrdered,
        /// <summary>
        /// Will handle packets as they arrive.
        /// </summary>
        UnreliableUnordered
    }
}
