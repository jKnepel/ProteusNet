using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Modules;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serializing;
using System;
using UnityEngine;
using UnityEditor;

using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Managing
{
    public static class StaticNetworkManager
    {
        /// <summary>
        /// The transport instance defined by the configuration
        /// </summary>
        public static ATransport Transport => NetworkManager.Transport;
        
        /// <summary>
        /// The instance of the network object prefabs collection. Defines the identification
        /// for prefabs across the network.
        /// </summary>
        public static NetworkObjectPrefabs NetworkObjectPrefabs
        {
            get => NetworkManager.NetworkObjectPrefabs;
            set => NetworkManager.NetworkObjectPrefabs = value;
        }
        
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Transport"/>
        /// </summary>
        public static TransportConfiguration TransportConfiguration
        {
            get => NetworkManager.TransportConfiguration;
            set => NetworkManager.TransportConfiguration = value;
        }

        /// <summary>
        /// Settings for the serializer used when sending byte and struct data
        /// </summary>
        public static SerializerSettings SerializerSettings => NetworkManager.SerializerSettings;
        /// <summary>
        /// The configuration that will create the instance of the <see cref="SerializerSettings"/>
        /// </summary>
        public static SerializerConfiguration SerializerConfiguration
        {
            get => NetworkManager.SerializerConfiguration;
            set => NetworkManager.SerializerConfiguration = value;
        }

        /// <summary>
        /// The logger instance defined by the configuration
        /// </summary>
        public static Logger Logger => NetworkManager.Logger;
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Logger"/>
        /// </summary>
        public static LoggerConfiguration LoggerConfiguration
        {
            get => NetworkManager.LoggerConfiguration;
            set => NetworkManager.LoggerConfiguration = value;
        }
        
        /// <summary>
        /// List of modules currently registered with the network manager
        /// </summary>
        public static ModuleList Modules => NetworkManager.Modules;

        /// <summary>
        /// The instance of the local server, which provides access to the server's API, values and events
        /// </summary>
        public static Server Server => NetworkManager.Server;
        /// <summary>
        /// The instance of the local client, which provides access to the client's API, values and events
        /// </summary>
        public static Client Client => NetworkManager.Client;
        public static Objects Objects => NetworkManager.Objects;

        /// <summary>
        /// Whether a local server is started
        /// </summary>
        public static bool IsServer => NetworkManager.IsServer;
        /// <summary>
        /// Whether a local client is started and authenticated
        /// </summary>
        public static bool IsClient => NetworkManager.IsClient;
        /// <summary>
        /// Whether a local server is started or local client is authenticated
        /// </summary>
        public static bool IsOnline => NetworkManager.IsOnline;
        /// <summary>
        /// Whether a local server is started and local client is authenticated
        /// </summary>
        public static bool IsHost => NetworkManager.IsHost;

        /// <summary>
        /// Defines where the network manager can be used
        /// </summary>
        public static EManagerScope ManagerScope => NetworkManager.ManagerScope;
        /// <summary>
        /// Defines if the network manager can currently be used
        /// </summary>
        public static bool IsInScope => NetworkManager.IsInScope;

        /// <summary>
        /// Whether the local server or client is ticking automatically.
        /// This is only set once, when starting a local server or local client.
        /// Once manual ticks are used, automatic ticks will be disabled.
        /// </summary>
        public static bool UseAutomaticTicks => NetworkManager.UseAutomaticTicks;
        /// <summary>
        /// The tick rate used for the automatic ticks
        /// </summary>
        public static uint Tickrate => NetworkManager.Tickrate;
        /// <summary>
        /// The current tick number
        /// </summary>
        public static uint CurrentTick => NetworkManager.CurrentTick;

        /// <summary>
        /// Called when a tick was started. Contains the tick number as parameter
        /// </summary>
        public static event Action<uint> OnTickStarted
        {
            add => NetworkManager.OnTickStarted += value;
            remove => NetworkManager.OnTickStarted -= value;
        }
        /// <summary>
        /// Called when a tick was completed. Contains the tick number as parameter
        /// </summary>
        public static event Action<uint> OnTickCompleted
        {
            add => NetworkManager.OnTickCompleted += value;
            remove => NetworkManager.OnTickCompleted -= value;
        }
        /// <summary>
        /// Called when <see cref="Transport"/> was disposed
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action OnTransportDisposed
        {
            add => NetworkManager.OnTransportDisposed += value;
            remove => NetworkManager.OnTransportDisposed -= value;
        }
        /// <summary>
        /// Called when the local server received new data from the transport layer
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action<ServerReceivedData> OnServerReceivedData
        {
            add => NetworkManager.OnServerReceivedData += value;
            remove => NetworkManager.OnServerReceivedData -= value;
        }
        /// <summary>
        /// Called when the local client received new data from the transport layer
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action<ClientReceivedData> OnClientReceivedData
        {
            add => NetworkManager.OnClientReceivedData += value;
            remove => NetworkManager.OnClientReceivedData -= value;
        }
        /// <summary>
        /// Called when the local server's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action<ELocalConnectionState> OnServerStateUpdated
        {
            add => NetworkManager.OnServerStateUpdated += value;
            remove => NetworkManager.OnServerStateUpdated -= value;
        }
        /// <summary>
        /// Called when the local client's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action<ELocalConnectionState> OnClientStateUpdated
        {
            add => NetworkManager.OnClientStateUpdated += value;
            remove => NetworkManager.OnClientStateUpdated -= value;
        }
        /// <summary>
        /// Called when a remote client's transport state was updated
        /// </summary>
        /// <remarks>
        /// Should be ignored unless you specifically want to use transport layer data
        /// </remarks>
        public static event Action<uint, ERemoteConnectionState> OnConnectionUpdated
        {
            add => NetworkManager.OnConnectionUpdated += value;
            remove => NetworkManager.OnConnectionUpdated -= value;
        }

        /// <summary>
        /// Instance of the internal network manager held by the static context 
        /// </summary>
        public static NetworkManager NetworkManager { get; }
        
        static StaticNetworkManager()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode || !IsOnline) return;
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Play mode is not possible while the static network manager is online!");
            };
            
            NetworkManager = new(EManagerScope.Editor);
            if (!NetworkObjectPrefabs)
                NetworkObjectPrefabs = NetworkObjectPrefabs.Instance;
            NetworkManager.NetworkObjectPrefabs = NetworkObjectPrefabs;
            NetworkManager.TransportConfiguration = TransportConfiguration;
            NetworkManager.SerializerConfiguration = SerializerConfiguration;
            NetworkManager.LoggerConfiguration = LoggerConfiguration;
        }

        /// <summary>
        /// This method calls the transport's internal tick method, updating connections and
        /// incoming and outgoing packets.v 
        /// </summary>
        /// <remarks>
        /// Calling this method will disable automatic ticks in the transport settings.
        /// Only use this method if ticks are to be handled manually.
        /// </remarks>
        public static void Tick() => NetworkManager.Tick();

        /// <summary>
        /// Method to start a local server
        /// </summary>
        public static void StartServer() => NetworkManager.StartServer();
        /// <summary>
        /// Method to stop the local server
        /// </summary>
        public static void StopServer() => NetworkManager.StopServer();

        /// <summary>
        /// Method to start a local client
        /// </summary>
        public static void StartClient() => NetworkManager.StartClient();
        /// <summary>
        /// Method to stop the local client 
        /// </summary>
        public static void StopClient() => NetworkManager.StopClient();

        /// <summary>
        /// Method to start both the local server and client
        /// </summary>
        public static void StartHost() => NetworkManager.StartHost();
        /// <summary>
        /// Method to stop both the local server and client
        /// </summary>
        public static void StopHost() =>NetworkManager.StopHost();
    }
}
