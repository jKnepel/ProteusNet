using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Modules;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serialising;
using System;
using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Managing
{
    public interface INetworkManager
    {
        #region fields
        
        /// <summary>
        /// The transport instance, which will be used for sending and receiving data
        /// and managing internal connections
        /// </summary>
        Transport Transport { get; }
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Transport"/>
        /// </summary>
        TransportConfiguration TransportConfiguration { get; set; }
        
        /// <summary>
        /// Settings for the serializer used when sending byte and struct data
        /// </summary>
        SerializerSettings SerializerSettings { get; }
        /// <summary>
        /// The configuration that will create the instance of the <see cref="SerializerSettings"/>
        /// </summary>
        SerializerConfiguration SerializerConfiguration { get; set; }
        
        /// <summary>
        /// The logger instance, which will be used for saving and displaying messages
        /// </summary>
        Logger Logger { get; }
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Logger"/>
        /// </summary>
        LoggerConfiguration LoggerConfiguration { get; set; }
        
        /// <summary>
        /// List of modules currently registered with the network manager
        /// </summary>
        ModuleList Modules { get; }
        
        /// <summary>
        /// The instance of the local server, which provides access to the server's API, values and events
        /// </summary>
        Server Server { get; }
        /// <summary>
        /// The instance of the local client, which provides access to the client's API, values and events
        /// </summary>
        Client Client { get; }
        
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
        /// Whether the local server or client is ticking automatically.
        /// This is only set once, when starting a local server or local client.
        /// Once manual ticks are used, automatic ticks will be disabled.
        /// </summary>
        bool UseAutomaticTicks { get; }
        /// <summary>
        /// The tick rate used for the automatic ticks
        /// </summary>
        uint Tickrate { get; }
        /// <summary>
        /// The current tick number
        /// </summary>
        uint CurrentTick { get; }
        
        #endregion
        
        #region events

        /// <summary>
        /// Called when <see cref="Transport"/> was disposed
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action OnTransportDisposed;
        /// <summary>
        /// Called when the local server received new data from the transport layer
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<ServerReceivedData> OnServerReceivedData;
        /// <summary>
        /// Called when the local client received new data from the transport layer
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<ClientReceivedData> OnClientReceivedData;
        /// <summary>
        /// Called when the local server's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<ELocalConnectionState> OnServerStateUpdated;
        /// <summary>
        /// Called when the local client's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<ELocalConnectionState> OnClientStateUpdated;
        /// <summary>
        /// Called when a remote client's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<uint, ERemoteConnectionState> OnConnectionUpdated;
        /// <summary>
        /// Called when a new log was added by the transport
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public event Action<string, EMessageSeverity> OnTransportLogAdded;
        /// <summary>
        /// Called when a tick was started. Contains the tick number as parameter
        /// </summary>
        event Action<uint> OnTickStarted;
        /// <summary>
        /// Called when a tick was completed. Contains the tick number as parameter
        /// </summary>
        event Action<uint> OnTickCompleted;
        
        #endregion
        
        #region methods

        /// <summary>
        /// This method calls the transport's internal tick method, updating connections and
        /// incoming and outgoing packets.
        /// </summary>
        /// <remarks>
        /// Calling this method will disable automatic ticks in the transport settings.
        /// Only use this method if ticks are to be handled manually.
        /// </remarks>
        void Tick();

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
