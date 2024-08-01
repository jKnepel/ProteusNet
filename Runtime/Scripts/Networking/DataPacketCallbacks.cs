using System;

namespace jKnepel.ProteusNet.Networking
{
    /// <summary>
    /// A struct containing the data of a received byte packet
    /// </summary>
    public struct ByteData
    {
        /// <summary>
        /// The received byte data
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// The ID of the sender
        /// </summary>
        public uint SenderID;
        /// <summary>
        /// The number of the tick in which it was received
        /// </summary>
        public uint Tick;
        /// <summary>
        /// The timestamp of when it was received
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// The channel on which it was received
        /// </summary>
        public ENetworkChannel Channel;
    }

    /// <summary>
    /// A struct containing the data of a received struct packet
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructData<T>
    {
        /// <summary>
        /// The received struct data
        /// </summary>
        public T Data;
        /// <summary>
        /// The ID of the sender
        /// </summary>
        public uint SenderID;
        /// <summary>
        /// The number of the tick in which it was received
        /// </summary>
        public uint Tick;
        /// <summary>
        /// The timestamp of when it was received
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// The channel on which it was received
        /// </summary>
        public ENetworkChannel Channel;
    }
}
