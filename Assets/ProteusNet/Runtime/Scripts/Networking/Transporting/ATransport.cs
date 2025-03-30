using System;
using System.Net;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    [Serializable]
    public abstract class ATransport : IDisposable
    {
        /// <summary>
        /// The endpoint of the local server or the server the local client is connected to
        /// </summary>
        public abstract IPEndPoint ServerEndpoint { get; }
        /// <summary>
        /// The maximum number of connections allowed by the local server. 
        /// </summary>
        public abstract uint MaxNumberOfClients { get; }
        
        /// <summary>
        /// Whether a local server is started
        /// </summary>
        public abstract bool IsServer { get; }
        /// <summary>
        /// Whether a local client is started
        /// </summary>
        public abstract bool IsClient { get; }

        /// <summary>
        /// The current connection state of the local server
        /// </summary>
        public abstract ELocalConnectionState LocalServerState { get; }
        /// <summary>
        /// The current connection state of the local client
        /// </summary>
        public abstract ELocalConnectionState LocalClientState { get; }
        
        /// <summary>
        /// Called when the local server has received data
        /// </summary>
        public abstract event Action<ServerReceivedData> OnServerReceivedData;
        /// <summary>
        /// Called when the local client has received data
        /// </summary>
        public abstract event Action<ClientReceivedData> OnClientReceivedData;
        /// <summary>
        /// Called when the local server's connection state has been updated
        /// </summary>
        public abstract event Action<ELocalConnectionState> OnServerStateUpdated;
        /// <summary>
        /// Called when the local client's connection state has been updated
        /// </summary>
        public abstract event Action<ELocalConnectionState> OnClientStateUpdated;
        /// <summary>
        /// Called when a remote client's connection state has been updated
        /// </summary>
        public abstract event Action<uint, ERemoteConnectionState> OnConnectionUpdated;

        ~ATransport()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {}

        public abstract void Tick();
        public abstract void StartServer(string serverListenAddress, ushort port, uint maxNumberOfClients);
        public abstract void StopServer();
        public abstract void StartClient(string serverAddress, ushort port);
        public abstract void StopClient();
        public abstract void SendDataToServer(ArraySegment<byte> data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        public abstract void SendDataToClient(uint clientID, ArraySegment<byte> data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        public abstract void DisconnectClient(uint clientID);
        
        public abstract int GetRTTToServer();
        public abstract int GetRTTToClient(uint clientID);
        public abstract NetworkMetrics GetNetworkMetrics();
        public abstract NetworkMetrics GetNetworkMetricsToServer();
        public abstract NetworkMetrics GetNetworkMetricsToClient(uint clientID);
    }
    
    public struct ServerReceivedData
    {
        public uint ClientID;
        public ArraySegment<byte> Data;
        public ENetworkChannel Channel;
    }

    public struct ClientReceivedData
    {
        public ArraySegment<byte> Data;
        public ENetworkChannel Channel;
    }
    
    public class NetworkMetrics
    {
        public uint PacketSentCount;
        public uint PacketSentSize;
        public uint PacketReceivedCount;
        public uint PacketReceivedSize;

        public uint RTT;
        
        public uint PacketsDropped;
        public uint PacketsResent;
        public uint PacketsOutOfOrder;
        public uint PacketsDuplicated;

        public void AddNetworkMetrics(NetworkMetrics metrics)
        {
            if (metrics == null) return;
            
            PacketSentCount += metrics.PacketSentCount;
            PacketSentSize += metrics.PacketSentSize;
            PacketReceivedCount += metrics.PacketReceivedCount;
            PacketReceivedSize += metrics.PacketReceivedSize;

            RTT = Math.Max(RTT, metrics.RTT);
            
            PacketsDropped += metrics.PacketsDropped;
            PacketsResent += metrics.PacketsResent;
            PacketsOutOfOrder += metrics.PacketsOutOfOrder;
            PacketsDuplicated += metrics.PacketsDuplicated;
        }
    }

    public enum ELocalConnectionState
    {
        /// <summary>
        /// Signifies the start of a local connection
        /// </summary>
        Starting = 0,
        /// <summary>
        /// Signifies that a local connection has been successfully established
        /// </summary>
        Started = 1,
        /// <summary>
        /// Signifies that an established local connection is being closed
        /// </summary>
        Stopping = 2,
        /// <summary>
        /// Signifies that an established local connection was closed
        /// </summary>
        Stopped = 3
    }

    public enum ERemoteConnectionState
    {
        /// <summary>
        /// Signifies that a remote connection has been established
        /// </summary>
        Connected,
        /// <summary>
        /// Signifies that an established remote connection was closed
        /// </summary>
        Disconnected
    }
}
