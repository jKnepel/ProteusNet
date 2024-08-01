using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Modules;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serialising;
using System;
using UnityEngine;
using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Managing
{
    public class NetworkManager : INetworkManager, IDisposable
    {
        #region fields
        
        private Transport _transport;
        public Transport Transport
        {
            get => _transport;
            private set
            {
                if (value == _transport) return;
                
                if (_transport is not null)
                {
                    _transport.Dispose();
                    OnTransportDisposed?.Invoke();
                }
                
                _transport = value;
                if (_transport is null) return;
                _transport.OnServerReceivedData += ServerReceivedData;
                _transport.OnClientReceivedData += ClientReceivedData;
                _transport.OnServerStateUpdated += ServerStateUpdated;
                _transport.OnClientStateUpdated += ClientStateUpdated;
                _transport.OnConnectionUpdated += ConnectionUpdated;
                _transport.OnTransportLogAdded += TransportLogAdded;
                _transport.OnTickStarted += TickStarted;
                _transport.OnTickCompleted += TickCompleted;
                
                if (Logger is not null)
                    _transport.OnTransportLogAdded += Logger.Log;
            }
        }
        private TransportConfiguration _transportConfiguration;
        public TransportConfiguration TransportConfiguration
        {
            get => _transportConfiguration;
            set
            {
                if (value == _transportConfiguration) return;
                if (IsOnline)
                {
                    Debug.LogError("Can't change the configuration while a local connection is established!");
                    return;
                }
                
                _transportConfiguration = value;
                if (_transportConfiguration is not null)
                    Transport = _transportConfiguration.GetTransport();
            }
        }

        public SerializerSettings SerializerSettings
        {
            get; 
            private set;
        }
        private SerializerConfiguration _serializerConfiguration;
        public SerializerConfiguration SerializerConfiguration
        {
            get => _serializerConfiguration;
            set
            {
                if (value == _serializerConfiguration) return;
                if (IsOnline)
                {
                    Debug.LogError("Can't change the configuration while a local connection is established!");
                    return;
                }
                
                _serializerConfiguration = value;
                if (_serializerConfiguration is not null)
                    SerializerSettings = _serializerConfiguration.Settings;
            }
        }

        private Logger _logger;
        public Logger Logger
        {
            get => _logger;
            private set
            {
                if (value == _logger) return;
                if (_logger is not null)
                    OnTransportLogAdded -= Logger.Log;

                _logger = value;
                if (_logger is not null)
                    OnTransportLogAdded += Logger.Log;
            }
        }
        private LoggerConfiguration _loggerConfiguration;
        public LoggerConfiguration LoggerConfiguration
        {
            get => _loggerConfiguration;
            set
            {
                if (value == _loggerConfiguration) return;
                if (IsOnline)
                {
                    Debug.LogError("Can't change the configuration while a local connection is established!");
                    return;
                }
                
                _loggerConfiguration = value;
                if (_loggerConfiguration is not null)
                    Logger = _loggerConfiguration.GetLogger();
            }
        }

        public ModuleList Modules { get; } = new();

        public Server Server { get; private set; }
        public Client Client { get; private set; }

        public bool IsServer => Server.IsActive;
        public bool IsClient => Client.IsActive;
        public bool IsOnline => IsServer || IsClient;
        public bool IsHost => IsServer && IsClient;

        public event Action OnTransportDisposed;
        public event Action<ServerReceivedData> OnServerReceivedData;
        public event Action<ClientReceivedData> OnClientReceivedData;
        public event Action<ELocalConnectionState> OnServerStateUpdated;
        public event Action<ELocalConnectionState> OnClientStateUpdated;
        public event Action<uint, ERemoteConnectionState> OnConnectionUpdated;
        public event Action<string, EMessageSeverity> OnTransportLogAdded;
        public event Action<uint> OnTickStarted;
        public event Action<uint> OnTickCompleted;
        
        private bool _disposed;

        #endregion

        #region lifecycle

        public NetworkManager()
        {
            Server = new(this);
            Client = new(this);
        }

        ~NetworkManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Transport?.Dispose();
            }

            Transport = null;
            Logger = null;
            Server = null;
            Client = null;
        }

        public void Tick()
        {
            Transport?.Tick();
        }

        #endregion

        #region public methods

        public void StartServer()
        {
            if (TransportConfiguration == null)
            {
                Debug.LogError("The transport needs to be defined before a server can be started!");
                return;
            }

            Transport?.StartServer();
        }

        public void StopServer()
        {
            Transport?.StopServer();
        }

        public void StartClient()
        {
            if (TransportConfiguration == null)
            {
                Debug.LogError("The transport needs to be defined before a client can be started!");
                return;
            }
            
            Transport?.StartClient();
        }
        
        public void StopClient()
        {
            Transport?.StopClient();
        }

        public void StartHost()
        {
            StartServer();
            StartClient();
        }

        public void StopHost()
        {
            StopClient();
            StopServer();
        }

        #endregion
        
        #region transport event handlers

        private void ServerReceivedData(ServerReceivedData data) => OnServerReceivedData?.Invoke(data);
        private void ClientReceivedData(ClientReceivedData data) => OnClientReceivedData?.Invoke(data);
        private void ServerStateUpdated(ELocalConnectionState state) => OnServerStateUpdated?.Invoke(state);
        private void ClientStateUpdated(ELocalConnectionState state) => OnClientStateUpdated?.Invoke(state);
        private void ConnectionUpdated(uint id, ERemoteConnectionState state) => OnConnectionUpdated?.Invoke(id, state);
        private void TransportLogAdded(string log, EMessageSeverity sev) => OnTransportLogAdded?.Invoke(log, sev);
        private void TickStarted(uint tickNumber) => OnTickStarted?.Invoke(tickNumber);
        private void TickCompleted(uint tickNumber) => OnTickCompleted?.Invoke(tickNumber);

        #endregion
    }
}
