using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.Utilities;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public sealed partial class UnityTransport : Transport
    {
        #region fields
        
        private const int LOCAL_HOST_RTT = 0;
        
        private bool _disposed;

        private TransportSettings _settings;
        private IPEndPoint _serverEndpoint;
        private uint _maxNumberOfClients;
        
        private NetworkDriver _driver;
        private NetworkSettings _networkSettings;
        
        private RelayServerData _relayServerData;
        
        private readonly Dictionary<SendTarget, SendQueue> _outgoingMessages = new();

        private NetworkPipeline _unreliablePipeline;
        private NetworkPipeline _unreliableSequencedPipeline;
        private NetworkPipeline _reliablePipeline;
        private NetworkPipeline _reliableSequencedPipeline;

        private Dictionary<uint, NetworkConnection> _clientIDToConnection;
        private Dictionary<NetworkConnection, uint> _connectionToClientID;
        private uint _clientIDs = 1;
        
        private NetworkConnection _serverConnection;
        private uint _hostClientID; // client ID that the hosting server assigns its local client

        private ELocalConnectionState _serverState = ELocalConnectionState.Stopped;
        private ELocalConnectionState _clientState = ELocalConnectionState.Stopped;

        private ulong _incomingBytes;
        private ulong _outgoingBytes;
        
        public override ELocalConnectionState LocalServerState => _serverState;
        public override ELocalConnectionState LocalClientState => _clientState;

        public override bool IsServer => LocalServerState == ELocalConnectionState.Started;
        public override bool IsClient => LocalClientState == ELocalConnectionState.Started;
        public override bool IsOnline => IsServer || IsClient;
        public override bool IsHost => IsServer && IsClient;

        public override IPEndPoint ServerEndpoint => _serverEndpoint;
        public override uint MaxNumberOfClients => _maxNumberOfClients;

        public override event Action<ServerReceivedData> OnServerReceivedData;
        public override event Action<ClientReceivedData> OnClientReceivedData;
        public override event Action<ELocalConnectionState> OnServerStateUpdated;
        public override event Action<ELocalConnectionState> OnClientStateUpdated;
        public override event Action<uint, ERemoteConnectionState> OnConnectionUpdated;

        public override event Action<string, EMessageSeverity> OnTransportLogged;
        public override event Action<ulong, ulong> OnClientTrafficAdded;
        public override event Action<ulong, ulong> OnServerTrafficAdded;

        #endregion
        
        #region lifecycle

        public UnityTransport(TransportSettings settings)
        {
            _settings = settings;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                _serverState = ELocalConnectionState.Stopped;
                _clientState = ELocalConnectionState.Stopped;
                _clientIDToConnection = null;
                _connectionToClientID = null;
                _serverConnection = default;
            }

            // TODO : clean/flush outgoing messages
            CleanOutgoingMessages();
            DisposeInternals();

            _disposed = true;
        }

        public override void StartServer()
        {
            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                OnTransportLogged?.Invoke("Failed to start the server, there already exists a local server.", EMessageSeverity.Error);
                return;
            }

            SetLocalServerState(ELocalConnectionState.Starting);
            
            InitialiseSettings();

            var port = _settings.Port == 0 ? NetworkUtilities.FindNextAvailablePort() : _settings.Port;
            NetworkEndpoint endpoint = default;
            switch (_settings.ProtocolType)
            {
                case EProtocolType.UnityTransport:
                    if (!string.IsNullOrEmpty(_settings.ServerListenAddress))
                    {
                        if (!NetworkEndpoint.TryParse(_settings.ServerListenAddress, port, out endpoint))
                            NetworkEndpoint.TryParse(_settings.ServerListenAddress, port, out endpoint, NetworkFamily.Ipv6);
                    }
                    else
                    {
                        endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
                    }
                    break;
                case EProtocolType.UnityRelayTransport:
                    if (_relayServerData.Equals(default(RelayServerData)))
                    {
                        SetLocalServerState(ELocalConnectionState.Stopping);
                        DisposeInternals();
                        OnTransportLogged?.Invoke("The relay server data needs to be set before a server can be started using the relay protocol.", EMessageSeverity.Error);
                        SetLocalServerState(ELocalConnectionState.Stopped);
                        return;
                    }

                    _networkSettings.WithRelayParameters(ref _relayServerData, (int)_settings.HeartbeatTimeoutMS);
                    endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
                    break;
            }
            
            if (endpoint.Family == NetworkFamily.Invalid)
            {
                SetLocalServerState(ELocalConnectionState.Stopping);
                OnTransportLogged?.Invoke("The given local or remote address uses an invalid IP family.", EMessageSeverity.Error);
                SetLocalServerState(ELocalConnectionState.Stopped);
                return;
            }
            
            InitialiseDrivers();
            
            if (_driver.Bind(endpoint) != 0)
            {
                SetLocalServerState(ELocalConnectionState.Stopping);
                DisposeInternals();
                OnTransportLogged?.Invoke($"Failed to bind server to local address {endpoint.Address} and port {endpoint.Port}.", EMessageSeverity.Error);
                SetLocalServerState(ELocalConnectionState.Stopped);
                return;
            }

            _serverEndpoint = ParseNetworkEndpoint(endpoint);
            _maxNumberOfClients = _settings.MaxNumberOfClients;
            _clientIDs = 1;
            _clientIDToConnection = new();
            _connectionToClientID = new();
            
            _driver.Listen();
            SetLocalServerState(ELocalConnectionState.Started);
        }

        public override void StopServer()
        {
            if (LocalServerState is ELocalConnectionState.Stopping or ELocalConnectionState.Stopped) return;
            if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started) 
                StopHostClient();
            
            SetLocalServerState(ELocalConnectionState.Stopping);

            var conns = _clientIDToConnection.Values.ToArray();
            foreach (var conn in conns)
            {
                if (conn.GetState(_driver) == NetworkConnection.State.Disconnected) continue;
                // TODO : flush send queue
                CleanOutgoingMessages(conn);
                _driver.Disconnect(conn);
                while (_driver.PopEventForConnection(conn, out _) != NetworkEvent.Type.Empty) {}
            }

            _serverEndpoint = null;
            _maxNumberOfClients = 0;
            _clientIDToConnection = null;
            _connectionToClientID = null;
            
            _driver.ScheduleUpdate().Complete();
            DisposeInternals();

            SetLocalServerState(ELocalConnectionState.Stopped);
        }

        public override void StartClient()
        {
            if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                OnTransportLogged?.Invoke("Failed to start the client, there already exists a local client.", EMessageSeverity.Error);
                return;
            }

            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                StartHostClient();
                return;
            }
            
            SetLocalClientState(ELocalConnectionState.Starting);

            InitialiseSettings();
            
            NetworkEndpoint serverEndpoint = default;
            switch (_settings.ProtocolType)
            {
                case EProtocolType.UnityTransport:
                    if (!NetworkEndpoint.TryParse(_settings.Address, _settings.Port, out serverEndpoint))
                        NetworkEndpoint.TryParse(_settings.Address, _settings.Port, out serverEndpoint, NetworkFamily.Ipv6);
                    break;
                case EProtocolType.UnityRelayTransport:
                    if (_relayServerData.Equals(default(RelayServerData)))
                    {
                        SetLocalServerState(ELocalConnectionState.Stopping);
                        DisposeInternals();
                        OnTransportLogged?.Invoke("The relay server data needs to be set before a client can be started using the relay protocol.", EMessageSeverity.Error);
                        SetLocalServerState(ELocalConnectionState.Stopped);
                        return;
                    }

                    _networkSettings.WithRelayParameters(ref _relayServerData, (int)_settings.HeartbeatTimeoutMS);
                    serverEndpoint = _relayServerData.Endpoint;
                    break;
            }
            
            if (serverEndpoint.Family == NetworkFamily.Invalid)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                OnTransportLogged?.Invoke("The server address is invalid.", EMessageSeverity.Error);
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }

            InitialiseDrivers();
            
            var localEndpoint = serverEndpoint.Family == NetworkFamily.Ipv4 ? NetworkEndpoint.AnyIpv4 : NetworkEndpoint.AnyIpv6;
            if (_driver.Bind(localEndpoint) != 0)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                DisposeInternals();
                OnTransportLogged?.Invoke($"Failed to bind client to local address {localEndpoint.Address} and port {localEndpoint.Port}", EMessageSeverity.Error);
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }
            
            _serverEndpoint = ParseNetworkEndpoint(serverEndpoint);
            _maxNumberOfClients = _settings.MaxNumberOfClients;

            _serverConnection = _driver.Connect(serverEndpoint);
        }

        public override void StopClient()
        {
            if (LocalClientState is ELocalConnectionState.Stopping or ELocalConnectionState.Stopped) return;
            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                StopHostClient();
                return;
            }

            SetLocalClientState(ELocalConnectionState.Stopping);
            
            // TODO : flush send queue
            CleanOutgoingMessages(_serverConnection);
            if (_driver.Disconnect(_serverConnection) == 0)
            {
            }
            
            _serverEndpoint = null;
            _maxNumberOfClients = 0;
            _serverConnection = default;
            _clientIDToConnection = null;
            _connectionToClientID = null;
            
            _driver.ScheduleUpdate().Complete();
            DisposeInternals();

            SetLocalClientState(ELocalConnectionState.Stopped);
        }

        private void StartHostClient()
        {
            SetLocalClientState(ELocalConnectionState.Starting);
            
            if (_clientIDToConnection.Count >= _maxNumberOfClients)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                OnTransportLogged?.Invoke("Maximum number of clients reached. Server cannot accept the connection.", EMessageSeverity.Error);
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }
            
            _hostClientID = _clientIDs++;
            SetLocalClientState(ELocalConnectionState.Started);
            OnConnectionUpdated?.Invoke(_hostClientID, ERemoteConnectionState.Connected);
        }

        private void StopHostClient()
        {
            SetLocalClientState(ELocalConnectionState.Stopping);
            SetLocalClientState(ELocalConnectionState.Stopped);
            OnConnectionUpdated?.Invoke(_hostClientID, ERemoteConnectionState.Disconnected);
            _hostClientID = 0;
        }

        public override void DisconnectClient(uint clientID)
        {
            if (!IsServer)
            {
                OnTransportLogged?.Invoke("The server has to be started to disconnect a client.", EMessageSeverity.Error);
                return;
            }

            if (IsHost && clientID == _hostClientID)
            {
                StopHostClient();
                return;
            }

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
            {
                OnTransportLogged?.Invoke($"The client with the ID {clientID} does not exist", EMessageSeverity.Error);
                return;
            }
            
            if (_driver.GetConnectionState(conn) != NetworkConnection.State.Disconnected)
            {
                // TODO : flush send queues
                CleanOutgoingMessages(conn);
                _connectionToClientID.Remove(conn, out _);
                _clientIDToConnection.Remove(clientID);
                _driver.Disconnect(conn);
                while (_driver.PopEventForConnection(conn, out _) != NetworkEvent.Type.Empty) {}
            }
            
            OnConnectionUpdated?.Invoke(clientID, ERemoteConnectionState.Disconnected);
        }

        public void SetRelayServerData(RelayServerData relayServerData)
        {
            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                OnTransportLogged?.Invoke(
                    "Relay server data should not be set while a local connection is already active!" +
                    "If you are setting the relay data on the client side of a host, you can ignore this warning, " +
                    "but setting the relay data again as client is unnecessary."
                    , EMessageSeverity.Warning);
            }
            
            _relayServerData = relayServerData;
            _settings.ProtocolType = EProtocolType.UnityRelayTransport;
        }

        public override void Tick()
        {
            if (!_driver.IsCreated) return;

            IterateIncoming();
            IterateOutgoing();
            
            if (IsServer)
                OnServerTrafficAdded?.Invoke(_incomingBytes, _outgoingBytes);
            else
                OnClientTrafficAdded?.Invoke(_incomingBytes, _outgoingBytes);
            _incomingBytes = _outgoingBytes = 0;
        }

        #endregion
        
        #region incoming

        private void IterateIncoming()
        {
            _driver.ScheduleUpdate().Complete();

            while (_driver.IsCreated && AcceptConnection()) {}
            while (_driver.IsCreated && ProcessEvent()) {}
        }

        private bool AcceptConnection()
        {
            var conn = _driver.Accept();
            if (conn == default) return false;

            var numberOfConnectedClients = _clientIDToConnection.Count;
            if (IsHost) numberOfConnectedClients++;
            if (numberOfConnectedClients >= _maxNumberOfClients)
            {
                _driver.Disconnect(conn);
                while (_driver.PopEventForConnection(conn, out _) != NetworkEvent.Type.Empty) {}
                return false;
            }
            
            var clientID = _clientIDs++;
            _clientIDToConnection[clientID] = conn;
            _connectionToClientID[conn] = clientID;
            OnConnectionUpdated?.Invoke(clientID, ERemoteConnectionState.Connected);
            
            return true;
        }

        private bool ProcessEvent()
        {
            var eventType = _driver.PopEvent(out var conn, out var reader, out var pipe);

            switch (eventType)
            {
                case NetworkEvent.Type.Data:
                    HandleData(conn, reader, pipe);
                    return true;
                case NetworkEvent.Type.Connect:
                    SetLocalClientState(ELocalConnectionState.Started);
                    return true;
                case NetworkEvent.Type.Disconnect:
                    if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started && conn.Equals(_serverConnection))
                    {
                        SetLocalClientState(ELocalConnectionState.Stopping);
                        // TODO : flush send queues
                        CleanOutgoingMessages();
                        DisposeInternals();
                        SetLocalClientState(ELocalConnectionState.Stopped);
                    }
                    else if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
                    {
                        // TODO : flush send queues
                        CleanOutgoingMessages(conn);
                        _connectionToClientID.Remove(conn, out var clientID);
                        _clientIDToConnection.Remove(clientID);
                        OnConnectionUpdated?.Invoke(clientID, ERemoteConnectionState.Disconnected);
                    }
                    // TODO : handle reason
                    return true;
                case NetworkEvent.Type.Empty:
                default:
                    return false;
            }
        }

        private void HandleData(NetworkConnection conn, DataStreamReader reader, NetworkPipeline pipe)
        {
            var data = new byte[reader.Length];
            unsafe
            {
                fixed (byte* dataPtr = data)
                {
                    reader.ReadBytesUnsafe(dataPtr, reader.Length);
                }
            }

            if (IsServer)
            {
                OnServerReceivedData?.Invoke(new()
                {
                    ClientID = _connectionToClientID[conn],
                    Data = data,
                    Channel = ParseChannelPipeline(pipe)
                });
            }
            else if (IsClient && conn.Equals(_serverConnection))
            {
                OnClientReceivedData?.Invoke(new()
                {
                    Data = data,
                    Channel = ParseChannelPipeline(pipe)
                });
            }
            
            _incomingBytes += (ulong)(reader.Length + _driver.MaxHeaderSize(pipe));
        }
        
        #endregion
        
        #region outgoing

        public override void SendDataToServer(byte[] data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsClient)
            {
                OnTransportLogged?.Invoke("The local client has to be started to send data to the server.", EMessageSeverity.Error);
                return;
            }
            
            if (IsHost)
            {
                OnServerReceivedData?.Invoke(new()
                {
                    ClientID = _hostClientID,
                    Data = data,
                    Channel = channel
                });
                return;
            }

            SendTarget target = new(_serverConnection, ParseChannelPipeline(channel));
            if (!_outgoingMessages.TryGetValue(target, out var queue))
                _outgoingMessages[target] = queue = new(1);

            queue.Enqueue(new(data, Allocator.Persistent));
        }

        public override void SendDataToClient(uint clientID, byte[] data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsServer)
            {
                OnTransportLogged?.Invoke("The server has to be started to send data to clients.", EMessageSeverity.Error);
                return;
            }
            
            if (IsHost && clientID == _hostClientID)
            {
                OnClientReceivedData?.Invoke(new()
                {
                    Data = data,
                    Channel = channel
                });
                return;
            }

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
            {
                OnTransportLogged?.Invoke($"The client with the ID {clientID} does not exist!", EMessageSeverity.Error);
                return;
            }
            
            SendTarget target = new(conn, ParseChannelPipeline(channel));
            if (!_outgoingMessages.TryGetValue(target, out var queue))
                _outgoingMessages[target] = queue = new(1);

            queue.Enqueue(new(data, Allocator.Persistent));
        }

        private void IterateOutgoing()
        {
            foreach (var (sendTarget, sendQueue) in _outgoingMessages)
            {
                if (!_driver.IsCreated) return;

                /* TODO : implement once message batching works
                 new SendQueueJob
                 {
                    Driver = _driver.ToConcurrent(),
                    Target = sendTarget,
                    Queue = sendQueue
                 }.Run();
                 */
                
                while (sendQueue.Count > 0)
                {
                    var result = _driver.BeginSend(sendTarget.Pipeline, sendTarget.Connection, out var writer);
                    if (result != (int)StatusCode.Success)
                    {
                        OnTransportLogged?.Invoke($"Sending data start failed: {ParseStatusCode(result)}", EMessageSeverity.Error);
                        return;
                    }

                    var data = sendQueue.Peek();
                    writer.WriteBytes(data);
                    result = _driver.EndSend(writer);

                    if (result == data.Length)
                    {
                        sendQueue.Dequeue().Dispose();
                        _outgoingBytes += (ulong)(result + _driver.MaxHeaderSize(sendTarget.Pipeline));
                        continue;
                    }

                    if (result != (int)StatusCode.NetworkSendQueueFull)
                    {
                        sendQueue.Dequeue().Dispose();
                        OnTransportLogged?.Invoke($"Sending data end failed: {ParseStatusCode(result)}", EMessageSeverity.Error);
                        return;
                    }

                    OnTransportLogged?.Invoke($"{ParseStatusCode(result)} Resend will be attempted next tick.", EMessageSeverity.Warning);
                    return;
                }
            }
        }
        
        public override int GetRTTToServer()
        {
            if (!IsClient) return 0;
            if (IsHost) return LOCAL_HOST_RTT;
            
            _driver.GetPipelineBuffers(
                _reliablePipeline, 
                NetworkPipelineStageId.Get<ReliableSequencedPipelineStage>(),
                _serverConnection,
                out _,
                out _,
                out var sharedBuffer
            );

            unsafe
            {
                var sharedContext = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();
                return sharedContext->RttInfo.LastRtt;
            }
        }

        public override int GetRTTToClient(uint clientID)
        {
            if (!IsServer) return 0;
            if (IsHost && clientID == _hostClientID) return LOCAL_HOST_RTT;

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
                return 0;
            
            _driver.GetPipelineBuffers(
                _reliablePipeline, 
                NetworkPipelineStageId.Get<ReliableSequencedPipelineStage>(),
                conn,
                out _,
                out _,
                out var sharedBuffer
            );

            unsafe
            {
                var sharedContext = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();
                return sharedContext->RttInfo.LastRtt;
            }
        }
        
        #endregion
        
        #region utility
        
        private void SetLocalServerState(ELocalConnectionState state)
        {
            if (_serverState == state) return;
            _serverState = state;
            OnServerStateUpdated?.Invoke(_serverState);
        }
        
        private void SetLocalClientState(ELocalConnectionState state)
        {
            if (_clientState == state) return;
            _clientState = state;
            OnClientStateUpdated?.Invoke(_clientState);
        }

        private void InitialiseSettings()
        {
            _networkSettings = new(Allocator.Persistent);
            _networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: (int)_settings.ConnectTimeoutMS,
                maxConnectAttempts: (int)_settings.MaxConnectAttempts,
                disconnectTimeoutMS: (int)_settings.DisconnectTimeoutMS,
                heartbeatTimeoutMS: (int)_settings.HeartbeatTimeoutMS,
                reconnectionTimeoutMS: (int)_settings.ReconnectionTimeoutMS
            );
            _networkSettings.WithFragmentationStageParameters(
                payloadCapacity: (int)_settings.PayloadCapacity
            );
            _networkSettings.WithReliableStageParameters(
                windowSize: (int)_settings.WindowSize,
                minimumResendTime: (int)_settings.MinimumResendTime,
                maximumResendTime: (int)_settings.MaximumResendTime
            );
        }

        private void InitialiseDrivers()
        {
            _driver = NetworkDriver.Create(_networkSettings);
            _unreliablePipeline = NetworkPipeline.Null;
            _unreliableSequencedPipeline = _driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            _reliablePipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage));
            _reliableSequencedPipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        private void DisposeInternals()
        {
            if (_driver.IsCreated)
            {
                _driver.Dispose();
                _driver = default;
            }
            if (_networkSettings.IsCreated)
            {
                _networkSettings.Dispose();
                _networkSettings = default;
            }
            
            _unreliablePipeline = NetworkPipeline.Null;
            _unreliableSequencedPipeline = NetworkPipeline.Null;
            _reliablePipeline = NetworkPipeline.Null;
            _reliableSequencedPipeline = NetworkPipeline.Null;
        }

        private NetworkPipeline ParseChannelPipeline(ENetworkChannel channel)
        {
            return channel switch
            {
                ENetworkChannel.ReliableOrdered => _reliableSequencedPipeline,
                ENetworkChannel.ReliableUnordered => _reliablePipeline,
                ENetworkChannel.UnreliableOrdered => _unreliableSequencedPipeline,
                ENetworkChannel.UnreliableUnordered => _unreliablePipeline,
                _ => NetworkPipeline.Null
            };
        }

        private ENetworkChannel ParseChannelPipeline(NetworkPipeline pipeline)
        {
            if (pipeline == _reliableSequencedPipeline) return ENetworkChannel.ReliableOrdered;
            if (pipeline == _reliablePipeline) return ENetworkChannel.ReliableUnordered;
            if (pipeline == _unreliableSequencedPipeline) return ENetworkChannel.UnreliableOrdered;
            if (pipeline == _unreliablePipeline) return ENetworkChannel.UnreliableUnordered;
            return default;
        }

        private void CleanOutgoingMessages()
        {
            foreach (var queue in _outgoingMessages.Values)
                queue.Dispose();
            _outgoingMessages.Clear();
        }

        private void CleanOutgoingMessages(NetworkConnection conn)
        {
            var sendTargets = new NativeList<SendTarget>(4, Allocator.Persistent);
            foreach (var kvp in _outgoingMessages)
            {
                if (kvp.Key.Connection.Equals(conn))
                {
                    sendTargets.Add(kvp.Key);
                }
            }

            foreach (var sendTarget in sendTargets)
            {
                _outgoingMessages.Remove(sendTarget, out var queue);                                            
                queue.Dispose();
            }

            sendTargets.Dispose();
        }

        private static IPEndPoint ParseNetworkEndpoint(NetworkEndpoint val)
        {
            var values = val.Address.Split(":");
            return new(IPAddress.Parse(values[0]), ushort.Parse(values[1]));
        }
        
        private static string ParseStatusCode(int code)
        {
            return (StatusCode)code switch
            {
                StatusCode.Success => 
                    "Operation completed successfully.",
                StatusCode.NetworkIdMismatch => 
                    "Connection is invalid.",
                StatusCode.NetworkVersionMismatch =>
                    "Connection is invalid. This is usually caused by an attempt to use a connection that has been already closed.",
                StatusCode.NetworkStateMismatch =>
                    "State of the connection is invalid for the operation requested. This is usually caused by an attempt to send on a connecting/closed connection.",
                StatusCode.NetworkPacketOverflow => 
                    "Packet is too large for the supported capacity.",
                StatusCode.NetworkSendQueueFull => 
                    "Packet couldn't be sent because the send queue is full.",
                StatusCode.NetworkDriverParallelForErr => 
                    "Attempted to process the same connection in different jobs.",
                StatusCode.NetworkSendHandleInvalid => 
                    "The DataStreamWriter is invalid.",
                StatusCode.NetworkReceiveQueueFull =>
                    "A message couldn't be received because the receive queue is full. This can only be returned through ReceiveErrorCode.",
                StatusCode.NetworkSocketError => 
                    "There was an error from the underlying low-level socket.",
                _ => string.Empty
            };
        }
        
        #endregion
    }
}
