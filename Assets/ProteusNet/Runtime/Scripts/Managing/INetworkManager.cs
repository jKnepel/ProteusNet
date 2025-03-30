using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using System;
using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Managing
{
    public enum EManagerScope
    {
        Runtime,
        Editor
    }
    
    public interface INetworkManager
    {
        #region fields
        
        /// <summary>
        /// The logger instance, which will be used for saving and displaying messages
        /// </summary>
        Logger Logger { get; }
        
        /// <summary>
        /// The instance of the network object prefabs collection. Defines the identification
        /// for prefabs across the network.
        /// </summary>
        NetworkObjectPrefabs NetworkObjectPrefabs { get; set; }
        
        /// <summary>
        /// The transport instance, which will be used for sending and receiving data
        /// and managing internal connections
        /// </summary>
        ATransport Transport { get; }
        
        /// <summary>
        /// The address to which the local client will attempt to connect with.
        /// </summary>
        public string ServerAddress { get; set; }
        /// <summary>
        /// The port to which the local client will attempt to connect with or the server will bind to locally.
        /// </summary>
        public ushort Port { get; set; }
        /// <summary>
        /// Address to which the local server will be bound. If no address is provided, the the 0.0.0.0 address
        /// will be used instead.
        /// </summary>
        public string ServerListenAddress { get; set; }
        /// <summary>
        /// The maximum number of connections allowed by the local server. 
        /// </summary>
        public uint MaxNumberOfClients { get; set; }
        
        /// <summary>
        /// The instance of the local server, which provides access to the server's API, values and events
        /// </summary>
        Server Server { get; }
        /// <summary>
        /// The instance of the local client, which provides access to the client's API, values and events
        /// </summary>
        Client Client { get; }
        Objects Objects { get; }
        
        /// <summary>
        /// Whether a local server is started
        /// </summary>
        bool IsServer { get; }
        /// <summary>
        /// Whether a local client is started and authenticated
        /// </summary>
        bool IsClient { get; }
        /// <summary>
        /// Whether a local server is started or local client is authenticated
        /// </summary>
        bool IsOnline { get; }
        /// <summary>
        /// Whether a local server is started and local client is authenticated
        /// </summary>
        bool IsHost { get; }
        
        /// <summary>
        /// Defines where the network manager can be used
        /// </summary>
        EManagerScope ManagerScope { get; }
        /// <summary>
        /// Defines if the network manager can currently be used
        /// </summary>
        bool IsInScope { get; }
        
        /// <summary>
        /// The rate at which updates are performed per second. These updates include all network events,
        /// incoming and outgoing packets and client connections.
        /// </summary>
        uint Tickrate { get; set; }
        /// <summary>
        /// The current tick number
        /// </summary>
        uint CurrentTick { get; }
        
        #endregion
        
        #region events

        /// <summary>
        /// Called when a tick was started. Contains the tick number as parameter
        /// </summary>
        event Action<uint> OnTickStarted;
        /// <summary>
        /// Called when a tick was completed. Contains the tick number as parameter
        /// </summary>
        event Action<uint> OnTickCompleted;
        /// <summary>
        /// Called when <see cref="Transport"/> was exchanged using the configuration
        /// </summary>
        event Action OnTransportExchanged;
        
        #endregion
        
        #region methods

        /// <summary>
        /// Method to start a local server
        /// </summary>
        void StartServer();
        /// <summary>
        /// Method to stop the local server
        /// </summary>
        void StopServer();

        /// <summary>
        /// Method to start a local client
        /// </summary>
        void StartClient();
        /// <summary>
        /// Method to stop the local client
        /// </summary>
        void StopClient();

        /// <summary>
        /// Method to start both the local server and client
        /// </summary>
        void StartHost();
        /// <summary>
        /// Method to stop both the local server and client
        /// </summary>
        void StopHost();
        
        #endregion
    }
}
