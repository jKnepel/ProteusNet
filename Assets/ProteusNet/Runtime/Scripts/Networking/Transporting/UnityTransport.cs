using jKnepel.ProteusNet.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.Utilities;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    [Serializable]
    public partial class UnityTransport : ATransport
    {
        #region fields
        
        private const int LOCAL_HOST_RTT = 0;
        private const int MAX_RELIABLE_THROUGHPUT = NetworkParameterConstants.MTU * 64 * 60 / 1000;
        
        private bool _disposed;

        private IPEndPoint _serverEndpoint;
        private uint _maxNumberOfClients;
        
        private NetworkDriver _driver;
        private NetworkSettings _networkSettings;
        
        private RelayServerData _relayServerData;
        
        private readonly Dictionary<SendTarget, SendQueue> _sendQueues = new();
        private readonly Dictionary<NetworkConnection, ReceiveQueue> _reliableReceiveQueues = new();

        private NetworkPipeline _unreliablePipeline;
        private NetworkPipeline _unreliableSequencedPipeline;
        private NetworkPipeline _reliablePipeline; // UTP does not support unsequenced reliable, this is just a second sequenced pipe
        private NetworkPipeline _reliableSequencedPipeline;

        private Dictionary<uint, NetworkConnection> _clientIDToConnection = new();
        private Dictionary<NetworkConnection, uint> _connectionToClientID = new();
        private uint _nextClientID = 1;
        
        private NetworkConnection _serverConnection;
        private uint _hostClientID; // client ID that the hosting server assigns its local client

        private ELocalConnectionState _serverState = ELocalConnectionState.Stopped;
        private ELocalConnectionState _clientState = ELocalConnectionState.Stopped;

        public override ELocalConnectionState LocalServerState => _serverState;
        public override ELocalConnectionState LocalClientState => _clientState;

        public override bool IsServer => LocalServerState == ELocalConnectionState.Started;
        public override bool IsClient => LocalClientState == ELocalConnectionState.Started;
        private bool IsHost => IsServer && IsClient;

        public override IPEndPoint ServerEndpoint => _serverEndpoint;

        public override uint MaxNumberOfClients => _maxNumberOfClients;

        public override event Action<ServerReceivedData> OnServerReceivedData;
        public override event Action<ClientReceivedData> OnClientReceivedData;
        public override event Action<ELocalConnectionState> OnServerStateUpdated;
        public override event Action<ELocalConnectionState> OnClientStateUpdated;
        public override event Action<uint, ERemoteConnectionState> OnConnectionUpdated;

        #endregion
        
        #region lifecycle

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

            CleanOutgoingMessages();
            DisposeInternals();

            _disposed = true;
        }

        public override void StartServer(string serverListenAddress, ushort port, uint maxNumberOfClients)
        {
            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                Debug.LogError("Failed to start the server, there already exists a local server.");
                return;
            }
            if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                Debug.LogError("Failed to start the server, there already exists a local client.");
                return;
            }

            SetLocalServerState(ELocalConnectionState.Starting);
            
            InitializeSettings();
            
            port = port == 0 ? NetworkUtilities.FindNextAvailablePort() : port;
            NetworkEndpoint endpoint = default;
            switch (ProtocolType)
            {
                case EProtocolType.UnityTransport:
                    if (!string.IsNullOrEmpty(serverListenAddress))
                    {
                        if (!NetworkEndpoint.TryParse(serverListenAddress, port, out endpoint))
                            NetworkEndpoint.TryParse(serverListenAddress, port, out endpoint, NetworkFamily.Ipv6);
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
                        Debug.LogError("The relay server data needs to be set before a server can be started using the relay protocol.");
                        SetLocalServerState(ELocalConnectionState.Stopped);
                        return;
                    }

                    _networkSettings.WithRelayParameters(ref _relayServerData, (int)HeartbeatTimeoutMS);
                    endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
                    break;
            }
            
            if (endpoint.Family == NetworkFamily.Invalid)
            {
                SetLocalServerState(ELocalConnectionState.Stopping);
                Debug.LogError("The given local or remote address uses an invalid IP family.");
                SetLocalServerState(ELocalConnectionState.Stopped);
                return;
            }
            
            InitializeDrivers();
            
            if (_driver.Bind(endpoint) != 0)
            {
                SetLocalServerState(ELocalConnectionState.Stopping);
                DisposeInternals();
                Debug.LogError($"Failed to bind server to local address {endpoint.Address} and port {endpoint.Port}.");
                SetLocalServerState(ELocalConnectionState.Stopped);
                return;
            }

            _serverEndpoint = ParseNetworkEndpoint(endpoint);
            _maxNumberOfClients = maxNumberOfClients;
            
            _driver.Listen();
            SetLocalServerState(ELocalConnectionState.Started);
        }

        public override void StopServer()
        {
            if (LocalServerState is ELocalConnectionState.Stopping or ELocalConnectionState.Stopped) return;
            if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started) 
                StopHostClient();
            
            SetLocalServerState(ELocalConnectionState.Stopping);
            
            // flush all batched messages and disconnect to the network
            FlushOutgoingMessages();
            foreach (var (_, conn) in _clientIDToConnection)
            {
                if (_driver.GetConnectionState(conn) == NetworkConnection.State.Disconnected) 
                    continue;
                _driver.Disconnect(conn);
                while (_driver.PopEventForConnection(conn, out _) != NetworkEvent.Type.Empty) {}
            }
            
            _driver.ScheduleUpdate().Complete();

            CleanOutgoingMessages();
            DisposeInternals();
            
            _serverEndpoint = null;
            _maxNumberOfClients = 0;
            _reliableReceiveQueues.Clear();
            _clientIDToConnection.Clear();
            _connectionToClientID.Clear();
            _nextClientID = 1;

            SetLocalServerState(ELocalConnectionState.Stopped);
        }

        public override void StartClient(string serverAddress, ushort port)
        {
            if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                Debug.LogError("Failed to start the client, there already exists a local client.");
                return;
            }

            if (LocalServerState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
            {
                StartHostClient();
                return;
            }
            
            SetLocalClientState(ELocalConnectionState.Starting);

            InitializeSettings();
            
            NetworkEndpoint serverEndpoint = default;
            switch (ProtocolType)
            {
                case EProtocolType.UnityTransport:
                    if (!NetworkEndpoint.TryParse(serverAddress, port, out serverEndpoint))
                        NetworkEndpoint.TryParse(serverAddress, port, out serverEndpoint, NetworkFamily.Ipv6);
                    break;
                case EProtocolType.UnityRelayTransport:
                    if (_relayServerData.Equals(default(RelayServerData)))
                    {
                        SetLocalServerState(ELocalConnectionState.Stopping);
                        DisposeInternals();
                        Debug.LogError("The relay server data needs to be set before a client can be started using the relay protocol.");
                        SetLocalServerState(ELocalConnectionState.Stopped);
                        return;
                    }

                    _networkSettings.WithRelayParameters(ref _relayServerData, (int)HeartbeatTimeoutMS);
                    serverEndpoint = _relayServerData.Endpoint;
                    break;
            }
            
            if (serverEndpoint.Family == NetworkFamily.Invalid)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                Debug.LogError("The server address is invalid.");
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }

            InitializeDrivers();
            
            var localEndpoint = serverEndpoint.Family == NetworkFamily.Ipv4 ? NetworkEndpoint.AnyIpv4 : NetworkEndpoint.AnyIpv6;
            if (_driver.Bind(localEndpoint) != 0)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                DisposeInternals();
                Debug.LogError($"Failed to bind client to local address {localEndpoint.Address} and port {localEndpoint.Port}");
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }
            
            _serverEndpoint = ParseNetworkEndpoint(serverEndpoint);
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
            
            // flush all batched messages and disconnect to the server
            FlushOutgoingMessages();
            _driver.Disconnect(_serverConnection);
            _driver.ScheduleUpdate().Complete();
            
            CleanOutgoingMessages();
            DisposeInternals();
            
            _serverEndpoint = null;
            _serverConnection = default;
            _reliableReceiveQueues.Clear();

            SetLocalClientState(ELocalConnectionState.Stopped);
        }

        private void StartHostClient()
        {
            SetLocalClientState(ELocalConnectionState.Starting);
            
            if (_clientIDToConnection.Count >= MaxNumberOfClients)
            {
                SetLocalClientState(ELocalConnectionState.Stopping);
                Debug.LogError("Maximum number of clients reached. Server cannot accept the connection.");
                SetLocalClientState(ELocalConnectionState.Stopped);
                return;
            }
            
            _hostClientID = _nextClientID++;
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
                Debug.LogError("The server has to be started to disconnect a client.");
                return;
            }

            if (IsHost && clientID == _hostClientID)
            {
                StopHostClient();
                return;
            }

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
            {
                Debug.LogError($"The client with the ID {clientID} does not exist");
                return;
            }
            
            if (_driver.GetConnectionState(conn) != NetworkConnection.State.Disconnected)
            {
                FlushOutgoingMessages(conn);
                CleanOutgoingMessages(conn);
                _connectionToClientID.Remove(conn);
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
                Debug.LogWarning(
                    "Relay server data should not be set while a local connection is already active." +
                    "If you are setting the relay data on the client side of a host, you can ignore this warning, " +
                    "but setting the relay data again as client is unnecessary.");
            }
            
            _relayServerData = relayServerData;
            ProtocolType = EProtocolType.UnityRelayTransport;
        }

        public override void Tick()
        {
            if (!_driver.IsCreated) return;

            IterateIncoming();
            IterateOutgoing();
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
            
            var numberOfConnectedClients = IsHost ? _clientIDToConnection.Count + 1 : _clientIDToConnection.Count;
            if (numberOfConnectedClients >= MaxNumberOfClients)
            {
                _driver.Disconnect(conn);
                while (_driver.PopEventForConnection(conn, out _) != NetworkEvent.Type.Empty) {}
                return false;
            }
            
            var clientID = _nextClientID++;
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
                    ReceiveMessage(conn, reader, pipe);
                    return true;
                case NetworkEvent.Type.Connect:
                    SetLocalClientState(ELocalConnectionState.Started);
                    return true;
                case NetworkEvent.Type.Disconnect:
                    if (LocalServerState is ELocalConnectionState.Started)
                    {   // remote client disconnected
                        CleanOutgoingMessages(conn);
                        _connectionToClientID.Remove(conn, out var clientID);
                        _clientIDToConnection.Remove(clientID);
                        OnConnectionUpdated?.Invoke(clientID, ERemoteConnectionState.Disconnected);
                    }
                    else if (LocalClientState is ELocalConnectionState.Starting or ELocalConnectionState.Started)
                    {   // failed to connect to server
                        SetLocalClientState(ELocalConnectionState.Stopping);
                        CleanOutgoingMessages();
                        DisposeInternals();
                        _serverEndpoint = null;
                        _serverConnection = default;
                        _reliableReceiveQueues.Clear();
                        SetLocalClientState(ELocalConnectionState.Stopped);
                    }
                    // TODO : handle reason
                    return true;
                case NetworkEvent.Type.Empty:
                default:
                    return false;
            }
        }

        private void ReceiveMessage(NetworkConnection conn, DataStreamReader reader, NetworkPipeline pipe)
        {
            ReceiveQueue queue;
            if (pipe == _reliablePipeline || pipe == _reliableSequencedPipeline)
            {   
                // reliable might not be fully contained within one event payload
                // therefore data should be cached in a queue and read once message is complete
                if (_reliableReceiveQueues.TryGetValue(conn, out queue))
                    queue.PushReader(reader);
                else
                    queue = _reliableReceiveQueues[conn] = new(reader);
            }
            else
            {
                queue = new(reader);
            }

            while (!queue.IsEmpty)
            {
                var message = queue.PopMessage();
                if (message == default)
                    break;
                
                if (IsServer)
                {
                    OnServerReceivedData?.Invoke(new()
                    {
                        ClientID = _connectionToClientID[conn],
                        Data = message,
                        Channel = ParseChannelPipeline(pipe)
                    });
                }
                else if (IsClient && conn.Equals(_serverConnection))
                {
                    OnClientReceivedData?.Invoke(new()
                    {
                        Data = message,
                        Channel = ParseChannelPipeline(pipe)
                    });
                }
            }
        }
        
        #endregion
        
        #region outgoing

        public override void SendDataToServer(ArraySegment<byte> data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsClient)
            {
                Debug.LogError("The local client has to be started to send data to the server.");
                return;
            }
            
            if (IsServer)
            {
                OnServerReceivedData?.Invoke(new()
                {
                    ClientID = _hostClientID,
                    Data = data,
                    Channel = channel
                });
                return;
            }
            
            AddSendMessage(_serverConnection, data, channel);
        }

        public override void SendDataToClient(uint clientID, ArraySegment<byte> data, ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsServer)
            {
                Debug.LogError("The server has to be started to send data to clients.");
                return;
            }
            
            if (IsClient && clientID == _hostClientID)
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
                Debug.LogError($"The client with the ID {clientID} does not exist.");
                return;
            }

            AddSendMessage(conn, data, channel);
        }

        private void AddSendMessage(NetworkConnection conn, ArraySegment<byte> data, ENetworkChannel channel)
        {
            var pipeline = ParseChannelPipeline(channel);
            var isUnreliable = pipeline == _unreliablePipeline || pipeline == _unreliableSequencedPipeline;

            if (isUnreliable && data.Count > PayloadCapacity)
            {
                Debug.LogError($"Attempted to send unreliable data ({data.Count}) larger than the allowed Payload Capacity ({PayloadCapacity})");
                return;
            }
            
            var sendTarget = new SendTarget(conn, pipeline, !isUnreliable);
            if (!_sendQueues.TryGetValue(sendTarget, out var queue))
            {
                // Set max capacity to prevent queues that take longer to send than the disconnect timeout
                var maxCapacity = DisconnectTimeoutMS * MAX_RELIABLE_THROUGHPUT;
                queue = new((int)Math.Max(maxCapacity, PayloadCapacity));
                _sendQueues.Add(sendTarget, queue);
            }

            if (queue.PushMessage(data)) 
                return;
            
            // handle cases where the message was over capacity
            if (pipeline == _reliablePipeline || pipeline == _reliableSequencedPipeline)
            {
                // message would take longer than disconnect timeout to send, causing automatic disconnect
                // just disconnect right away since a desync is guaranteed
                var clientID = _connectionToClientID[conn];
                Debug.LogError($"Couldn't add data of size {data.Count} to reliable send queue. Closing Connection.");
                    
                if (conn == _serverConnection)
                    StopClient();
                else
                    DisconnectClient(clientID);
            }
            else
            {
                // flush out queue to make space and send unreliable traffic anyway
                _driver.ScheduleFlushSend().Complete();
                SendMessage(sendTarget, queue);
                queue.PushMessage(data);
            }
        }

        private void SendMessage(SendTarget target, SendQueue queue)
        {
            if (!_driver.IsCreated)
                return;
            
            new SendQueueJob
            {
                Driver = _driver.ToConcurrent(),
                Target = target,
                Queue = queue
            }.Run();
        }

        private void IterateOutgoing()
        {
            if (!_driver.IsCreated)
                return;
            
            foreach (var (sendTarget, sendQueue) in _sendQueues)
            {
                SendMessage(sendTarget, sendQueue);
            }
        }
        
        #endregion
        
        #region metrics
        
        public override int GetRTTToServer()
        {
            if (!IsClient)
            {
                Debug.LogError("The local client has to be started to get the RTT to the server.");
                return -1;
            }
            if (IsServer) return LOCAL_HOST_RTT;
            
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
            if (!IsServer)
            {
                Debug.LogError("The local server has to be started to get the RTT to a client.");
                return -1;
            }
            if (IsClient && clientID == _hostClientID) return LOCAL_HOST_RTT;

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
            {
                Debug.LogError($"The client with the ID {clientID} does not exist.");
                return -2;
            }
            
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

        public override NetworkMetrics GetNetworkMetrics()
        {
            if (IsServer)
            {
                var metrics = new NetworkMetrics();
                foreach (var conn in _clientIDToConnection.Values)
                    metrics.AddNetworkMetrics(GetNetworkMetrics(conn));
                return metrics;
            }
            if (IsClient)
            {
                return GetNetworkMetrics(_serverConnection);
            }

            Debug.LogError("Metrics can only be retrieved once a local connection is active.");
            return null;
        }

        public override NetworkMetrics GetNetworkMetricsToServer()
        {
            if (!IsClient)
            {
                Debug.LogError("The local client has to be started to get the metrics to the server.");
                return null;
            }

            return GetNetworkMetrics(_serverConnection);
        }

        public override NetworkMetrics GetNetworkMetricsToClient(uint clientID)
        {
            if (!IsServer)
            {
                Debug.LogError("The local server has to be started to get the metrics to a client.");
                return null;
            }

            if (!_clientIDToConnection.TryGetValue(clientID, out var conn))
            {
                Debug.LogError($"The client with the ID {clientID} does not exist.");
                return null;
            }

            return GetNetworkMetrics(conn);
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

        private void InitializeSettings()
        {
            _networkSettings = new(Allocator.Persistent);
            _networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: (int)ConnectTimeoutMS,
                maxConnectAttempts: (int)MaxConnectAttempts,
                disconnectTimeoutMS: (int)DisconnectTimeoutMS,
                heartbeatTimeoutMS: (int)HeartbeatTimeoutMS,
                reconnectionTimeoutMS: (int)ReconnectionTimeoutMS
            );

            _networkSettings.WithFragmentationStageParameters(
                payloadCapacity: (int)PayloadCapacity + SendQueue.MESSAGE_OVERHEAD
            );
            _networkSettings.WithReliableStageParameters(
                windowSize: (int)WindowSize,
                minimumResendTime: (int)MinimumResendTime,
                maximumResendTime: (int)MaximumResendTime
            );
            
            if (NetworkSimulationState != ESimulationState.Off)
            {
                var mode = NetworkSimulationState switch
                {
                    ESimulationState.SendOnly => ApplyMode.SentPacketsOnly,
                    ESimulationState.ReceiveOnly => ApplyMode.ReceivedPacketsOnly,
                    ESimulationState.Always => ApplyMode.AllPackets,
                    _ => throw new ArgumentOutOfRangeException()
                };

                _networkSettings.WithSimulatorStageParameters(
                    mode: mode,
                    maxPacketCount: (int)MaxPacketCount,
                    maxPacketSize: (int)MaxPacketSize,
                    packetDelayMs: (int)PacketDelayMs,
                    packetJitterMs: (int)PacketJitterMs,
                    packetDropInterval: (int)PacketDropInterval,
                    packetDropPercentage: (int)(PacketDropPercentage * 100),
                    packetDuplicationPercentage: (int)(PacketDuplicationPercentage * 100),
                    fuzzFactor: (int)(FuzzFactor * 100),
                    fuzzOffset: (int)FuzzOffset,
                    randomSeed: (uint)System.Diagnostics.Stopwatch.GetTimestamp()
                );
            }
        }

        private void InitializeDrivers()
        {
            _driver = NetworkDriver.Create(_networkSettings);
            _driver.RegisterPipelineStage(new NetworkProfilerPipelineStage());

            if (NetworkSimulationState == ESimulationState.Off)
            {
                _unreliablePipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(NetworkProfilerPipelineStage));
                _unreliableSequencedPipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage), typeof(NetworkProfilerPipelineStage));
                _reliablePipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage), typeof(NetworkProfilerPipelineStage));
                _reliableSequencedPipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage), typeof(NetworkProfilerPipelineStage));
            }
            else
            {
                _unreliablePipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(SimulatorPipelineStage), typeof(NetworkProfilerPipelineStage));
                _unreliableSequencedPipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStage), typeof(NetworkProfilerPipelineStage));
                _reliablePipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage), typeof(NetworkProfilerPipelineStage));
                _reliableSequencedPipeline = _driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage), typeof(NetworkProfilerPipelineStage));
            }
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

        private void FlushOutgoingMessages()
        {
            foreach (var (target, queue) in _sendQueues)
                SendMessage(target, queue);
        }

        private void FlushOutgoingMessages(NetworkConnection conn)
        {
            foreach (var (target, queue) in _sendQueues)
            {
                if (target.Connection == conn)
                {
                    SendMessage(target, queue);
                }
            }
        }

        private void CleanOutgoingMessages()
        {
            foreach (var queue in _sendQueues.Values)
                queue.Dispose();
            _sendQueues.Clear();
        }

        private void CleanOutgoingMessages(NetworkConnection conn)
        {
            var sendTargets = new NativeList<SendTarget>(4, Allocator.Temp);
            foreach (var (target, _) in _sendQueues)
            {
                if (target.Connection.Equals(conn))
                {
                    sendTargets.Add(target);
                }
            }

            foreach (var sendTarget in sendTargets)
            {
                _sendQueues.Remove(sendTarget, out var queue);                                            
                queue.Dispose();
            }
        }
        
        private NetworkPipeline ParseChannelPipeline(ENetworkChannel channel)
        {
            switch (channel)
            {
                case ENetworkChannel.ReliableOrdered:
                    return _reliableSequencedPipeline;
                case ENetworkChannel.ReliableUnordered:
                    return _reliablePipeline;
                case ENetworkChannel.UnreliableOrdered:
                    return _unreliableSequencedPipeline;
                case ENetworkChannel.UnreliableUnordered:
                    return _unreliablePipeline;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ENetworkChannel ParseChannelPipeline(NetworkPipeline pipeline)
        {
            if (pipeline == _reliablePipeline) return ENetworkChannel.ReliableUnordered;
            if (pipeline == _reliableSequencedPipeline) return ENetworkChannel.ReliableOrdered;
            if (pipeline == _unreliableSequencedPipeline) return ENetworkChannel.UnreliableOrdered;
            if (pipeline == _unreliablePipeline) return ENetworkChannel.UnreliableUnordered;
            throw new ArgumentOutOfRangeException();
        }
        
        private NetworkMetrics GetNetworkMetrics(NetworkConnection conn)
        {
            if (_driver.GetConnectionState(conn) != NetworkConnection.State.Connected)
                return null;

            var metrics = new NetworkMetrics();

            {
                _driver.GetPipelineBuffers(
                    _reliablePipeline,
                    NetworkPipelineStageId.Get<NetworkProfilerPipelineStage>(),
                    conn,
                    out _,
                    out _,
                    out var sharedBuffer
                );
                unsafe
                {
                    var sharedContext = (NetworkProfilerContext*)sharedBuffer.GetUnsafePtr();
                    metrics.PacketSentCount += sharedContext->PacketSentCount;
                    metrics.PacketSentSize += sharedContext->PacketSentSize;
                    metrics.PacketReceivedCount += sharedContext->PacketReceivedCount;
                    metrics.PacketReceivedSize += sharedContext->PacketReceivedSize;

                    sharedContext->PacketSentCount = 0;
                    sharedContext->PacketSentSize = 0;
                    sharedContext->PacketReceivedCount = 0;
                    sharedContext->PacketReceivedSize = 0;
                }
            }
            {
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
                    metrics.RTT = (uint)sharedContext->RttInfo.LastRtt;
                    metrics.PacketsDropped += (uint)sharedContext->stats.PacketsDropped;
                    metrics.PacketsResent += (uint)sharedContext->stats.PacketsResent;
                    metrics.PacketsOutOfOrder += (uint)sharedContext->stats.PacketsOutOfOrder;
                    metrics.PacketsDuplicated += (uint)sharedContext->stats.PacketsDuplicated;
                }
            }
            {
                _driver.GetPipelineBuffers(
                    _unreliablePipeline,
                    NetworkPipelineStageId.Get<NetworkProfilerPipelineStage>(),
                    conn,
                    out _,
                    out _,
                    out var sharedBuffer
                );
                unsafe
                {
                    var sharedContext = (NetworkProfilerContext*)sharedBuffer.GetUnsafePtr();
                    metrics.PacketSentCount += sharedContext->PacketSentCount;
                    metrics.PacketSentSize += sharedContext->PacketSentSize;
                    metrics.PacketReceivedCount += sharedContext->PacketReceivedCount;
                    metrics.PacketReceivedSize += sharedContext->PacketReceivedSize;

                    sharedContext->PacketSentCount = 0;
                    sharedContext->PacketSentSize = 0;
                    sharedContext->PacketReceivedCount = 0;
                    sharedContext->PacketReceivedSize = 0;
                }
            }
            {
                _driver.GetPipelineBuffers(
                    _unreliableSequencedPipeline,
                    NetworkPipelineStageId.Get<NetworkProfilerPipelineStage>(),
                    conn,
                    out _,
                    out _,
                    out var sharedBuffer
                );
                unsafe
                {
                    var sharedContext = (NetworkProfilerContext*)sharedBuffer.GetUnsafePtr();
                    metrics.PacketSentCount += sharedContext->PacketSentCount;
                    metrics.PacketSentSize += sharedContext->PacketSentSize;
                    metrics.PacketReceivedCount += sharedContext->PacketReceivedCount;
                    metrics.PacketReceivedSize += sharedContext->PacketReceivedSize;

                    sharedContext->PacketSentCount = 0;
                    sharedContext->PacketSentSize = 0;
                    sharedContext->PacketReceivedCount = 0;
                    sharedContext->PacketReceivedSize = 0;
                }
            }
            {
                _driver.GetPipelineBuffers(
                    _unreliableSequencedPipeline, 
                    NetworkPipelineStageId.Get<UnreliableSequencedPipelineStage>(),
                    conn,
                    out _,
                    out _,
                    out var sharedBuffer
                );

                unsafe
                {
                    var sharedContext = (UnreliableSequencedPipelineStage.Statistics*)sharedBuffer.GetUnsafePtr();
                    metrics.PacketsDropped += (uint)sharedContext->NumPacketsDroppedNeverArrived;
                    metrics.PacketsOutOfOrder += (uint)sharedContext->NumPacketsCulledOutOfOrder;
                }
            }

            return metrics;
        }

        private static IPEndPoint ParseNetworkEndpoint(NetworkEndpoint val)
        {
            var values = val.Address.Split(":");
            return new(IPAddress.Parse(values[0]), ushort.Parse(values[1]));
        }
        
        private static FixedString128Bytes ParseStatusCode(int code)
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
