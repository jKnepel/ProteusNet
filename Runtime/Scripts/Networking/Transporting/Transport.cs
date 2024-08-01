using jKnepel.ProteusNet.Logging;
using System;
using System.Net;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public abstract class Transport : IDisposable
    {
        /// <summary>
        /// The endpoint of the local server or the server the local client is connected to
        /// </summary>
        public abstract IPEndPoint ServerEndpoint { get; }
        /// <summary>
        /// Max number of connected clients of the local server or the server the local client is connected to
        /// </summary>
        public abstract uint MaxNumberOfClients { get; }
        
        /// <summary>
        /// Whether a local server or client is started
        /// </summary>
        public abstract bool IsOnline { get; }
        /// <summary>
        /// Whether a local server is started
        /// </summary>
        public abstract bool IsServer { get; }
        /// <summary>
        /// Whether a local client is started
        /// </summary>
        public abstract bool IsClient { get; }
        /// <summary>
        /// Whether a local server and client is started
        /// </summary>
        public abstract bool IsHost { get; }

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
        
        /// <summary>
        /// Called when a log message was added in the underlying transport
        /// </summary>
        public abstract event Action<string, EMessageSeverity> OnTransportLogAdded;
        
        ~Transport()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract void StartServer();
        public abstract void StopServer();
        public abstract void StartClient();
        public abstract void StopClient();
        public abstract void StopNetwork();
        public abstract void Tick();
        public abstract void SendDataToServer(byte[] data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        public abstract void SendDataToClient(uint clientID, byte[] data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        public abstract void DisconnectClient(uint clientID);
        public abstract int GetRTTToServer();
        public abstract int GetRTTToClient(uint clientID);
    }
    
    public struct ServerReceivedData
    {
        public uint ClientID;
        public byte[] Data;
        public ENetworkChannel Channel;
    }

    public struct ClientReceivedData
    {
        public byte[] Data;
        public ENetworkChannel Channel;
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
