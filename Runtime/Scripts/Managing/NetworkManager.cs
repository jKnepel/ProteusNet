using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Modules;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Utilities;
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
                _transport.OnTransportLogged += TransportLogged;
                _transport.OnClientSentPacketLogged += ClientSentPacketLogged;
                _transport.OnServerSentPacketLogged += ServerSentPacketLogged;
                _transport.OnClientReceivedPacketLogged += ClientReceivedPacketLogged;
                _transport.OnServerReceivedPacketLogged += ServerReceivedPacketLogged;
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

        public Logger Logger { get; private set; }
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
        
        public EManagerScope ManagerScope { get; }

        public bool UseAutomaticTicks { get; private set; }
        public uint Tickrate { get; private set; }
        public uint CurrentTick { get; private set; }

        public event Action<uint> OnTickStarted;
        public event Action<uint> OnTickCompleted;
        public event Action OnTransportDisposed;
        public event Action<ServerReceivedData> OnServerReceivedData;
        public event Action<ClientReceivedData> OnClientReceivedData;
        public event Action<ELocalConnectionState> OnServerStateUpdated;
        public event Action<ELocalConnectionState> OnClientStateUpdated;
        public event Action<uint, ERemoteConnectionState> OnConnectionUpdated;
        
        private bool _disposed;
        private float _tickInterval;
        private float _elapsedInterval;

        #endregion

        #region lifecycle

        public NetworkManager(EManagerScope scope)
        {
            ManagerScope = scope;
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

            StopTicks();
            Transport?.Dispose();
            
            if (disposing)
            {
                Logger = null;
                Server = null;
                Client = null;
            }
        }

        public void Tick()
        {
            UseAutomaticTicks = false;
            
            OnTickStarted?.Invoke(CurrentTick);
            Transport?.Tick();
            OnTickCompleted?.Invoke(CurrentTick);
            CurrentTick++;
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

            StartTicks();
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

            StartTicks();
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
        
        #region private methods

        private void StartTicks()
        {
            if (IsOnline) return;
            
            UseAutomaticTicks = TransportConfiguration.Settings.AutomaticTicks;
            Tickrate = TransportConfiguration.Settings.Tickrate;
            CurrentTick = 0;
            _tickInterval = 1f / Tickrate;
            
            if (UseAutomaticTicks)
                StaticGameObject.OnUpdate += InternalAutomaticTick;
        }

        private void StopTicks()
        {
            UseAutomaticTicks = false;
            Tickrate = 0;
            CurrentTick = 0;
            _tickInterval = 0;
            StaticGameObject.OnUpdate -= InternalAutomaticTick;
        }
        
        private void InternalAutomaticTick()
        {
            if (!UseAutomaticTicks)
            {
                StaticGameObject.OnUpdate -= InternalAutomaticTick;
                return;
            }
            
            _elapsedInterval += Time.deltaTime;
            if (_elapsedInterval < _tickInterval) return;
            
            OnTickStarted?.Invoke(CurrentTick);
            Transport?.Tick();
            OnTickCompleted?.Invoke(CurrentTick);
            CurrentTick++;
            _elapsedInterval = 0;
        }
        
        private void ServerReceivedData(ServerReceivedData data) => OnServerReceivedData?.Invoke(data);
        private void ClientReceivedData(ClientReceivedData data) => OnClientReceivedData?.Invoke(data);
        private void ConnectionUpdated(uint id, ERemoteConnectionState state) => OnConnectionUpdated?.Invoke(id, state);
        private void ServerStateUpdated(ELocalConnectionState state)
        {
            OnServerStateUpdated?.Invoke(state);
            if (state == ELocalConnectionState.Stopped && !IsOnline)
                StopTicks();
        }
        private void ClientStateUpdated(ELocalConnectionState state)
        {
            OnClientStateUpdated?.Invoke(state);
            if (state == ELocalConnectionState.Stopped && !IsOnline)
                StopTicks();
        }
        private void TransportLogged(string log, EMessageSeverity sev)
        {
            switch (sev)
            {
                case EMessageSeverity.Log:
                    Logger?.Log(log);
                    break;
                case EMessageSeverity.Warning:
                    Logger?.LogWarning(log);
                    break;
                case EMessageSeverity.Error:
                    Logger?.LogError(log);
                    break;
                default:
                    return;
            }
        }
        private void ClientSentPacketLogged(ulong byteLength)
        {
            Logger?.LogClientSentPacket(CurrentTick, DateTime.Now, byteLength);
        }
        private void ServerSentPacketLogged(ulong byteLength)
        {
            Logger?.LogServerSentPacket(CurrentTick, DateTime.Now, byteLength);
        }
        private void ClientReceivedPacketLogged(ulong byteLength)
        {
            Logger?.LogClientReceivedPacket(CurrentTick, DateTime.Now, byteLength);
        }
        private void ServerReceivedPacketLogged(ulong byteLength)
        {
            Logger?.LogServerReceivedPacket(CurrentTick, DateTime.Now, byteLength);
        }

        #endregion
    }
}
