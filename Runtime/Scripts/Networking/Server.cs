using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking.Packets;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serializing;
using jKnepel.ProteusNet.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using Random = System.Random;

namespace jKnepel.ProteusNet.Networking
{
    public class Server
    {
        #region fields
        
        /// <summary>
        /// Whether the local server has been started or not
        /// </summary>
        public bool IsActive => LocalState == ELocalServerConnectionState.Started;
        
        /// <summary>
        /// Listen endpoint of the local server
        /// </summary>
        public IPEndPoint ServerEndpoint { get; private set; }

        /// <summary>
        /// Name of the local server
        /// </summary>
        public string Servername
        {
            get => _servername;
            set
            {
                if (value is null || value.Equals(_servername)) return;
                _servername = value;
                if (IsActive)
                    HandleServernameUpdated();
            }
        }
        /// <summary>
        /// Max number of connected clients of the local server
        /// </summary>
        public uint MaxNumberOfClients { get; private set; }
        /// <summary>
        /// The current connection state of the local server
        /// </summary>
        public ELocalServerConnectionState LocalState { get; private set; } = ELocalServerConnectionState.Stopped;
        /// <summary>
        /// The clients that are connected to the local server
        /// </summary>
        public ConcurrentDictionary<uint, ClientInformation> ConnectedClients { get; } = new();
        /// <summary>
        /// The number of clients connected to the local server
        /// </summary>
        public uint NumberOfConnectedClients => (uint)ConnectedClients.Count;
        
        /// <summary>
        /// Called when the local server's connection state has been updated
        /// </summary>
        public event Action<ELocalServerConnectionState> OnLocalStateUpdated;
        /// <summary>
        /// Called when the local server's connection state was started
        /// </summary>
        public event Action OnLocalServerStarted;
        /// <summary>
        /// Called by the local server when a new remote client has been authenticated
        /// </summary>
        public event Action<uint> OnRemoteClientConnected;
        /// <summary>
        /// Called by the local server when a remote client disconnected
        /// </summary>
        public event Action<uint> OnRemoteClientDisconnected;
        /// <summary>
        /// Called by the local server when a remote client updated its information
        /// </summary>
        public event Action<uint> OnRemoteClientUpdated;
        /// <summary>
        /// Called by the local server when it updated its information
        /// </summary>
        public event Action OnServerUpdated;
        
        private readonly ConcurrentDictionary<uint, byte[]> _authenticatingClients = new();
        private readonly Dictionary<uint, NetworkObject> _spawnedNetworkObjects = new();

        private readonly NetworkManager _networkManager;
        private string _servername = "New Server";
        
        #endregion

        public Server(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _networkManager.OnTransportDisposed += OnTransportDisposed;
            _networkManager.OnServerStateUpdated += OnServerStateUpdated;
            _networkManager.OnConnectionUpdated += OnRemoteConnectionUpdated;
            _networkManager.OnServerReceivedData += OnServerReceivedData;
        }

        public void DisconnectClient(uint clientID)
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local server must be started before clients can be disconnected!");
                return;
            }
            
            if (!ConnectedClients.ContainsKey(clientID))
            {
                _networkManager.Logger?.Log("No client with the given ID is connected to the local server!");
                return;
            }
            
            _networkManager?.Transport.DisconnectClient(clientID);
        }
        
        #region byte data
        
        private readonly ConcurrentDictionary<uint, Dictionary<int, DataPacketCallback>> _registeredServerByteDataCallbacks = new();

        /// <summary>
        /// Registers a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback"> Callback which will be invoked after byte data with the given id has been received</param>
        public void RegisterByteData(string byteID, Action<ByteData> callback)
        {
            var byteDataHash = Hashing.GetFNV1aHash32(byteID);

            if (!_registeredServerByteDataCallbacks.TryGetValue(byteDataHash, out var callbacks))
            {
                callbacks = new();
                _registeredServerByteDataCallbacks.TryAdd(byteDataHash, callbacks);
            }

            var key = callback.GetHashCode();
            var del = CreateByteDataDelegate(callback);
            if (!callbacks.ContainsKey(key))
                callbacks.TryAdd(key, del);
        }
        
        /// <summary>
        /// Unregisters a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">Callback which will be invoked after byte data with the given id has been received</param>
        public void UnregisterByteData(string byteID, Action<ByteData> callback)
        {
            var byteDataHash = Hashing.GetFNV1aHash32(byteID);

            if (!_registeredServerByteDataCallbacks.TryGetValue(byteDataHash, out var callbacks))
                return;

            callbacks.Remove(callback.GetHashCode(), out _);
        }
        
        /// <summary>
        /// Sends byte data with a given id from the local server to a given remote client.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        public void SendByteDataToClient(uint clientID, string byteID, byte[] byteData, 
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            SendByteDataToClients(new [] { clientID }, byteID, byteData, channel);
        }
        
        /// <summary>
        /// Sends byte data with a given id from the local server to all other remote clients.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        public void SendByteDataToAll(string byteID, byte[] byteData, 
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            SendByteDataToClients(ConnectedClients.Keys.ToArray(), byteID, byteData, channel);
        }
        
        /// <summary>
        /// Sends byte data with a given id from the local server to a list of remote clients.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        public void SendByteDataToClients(uint[] clientIDs, string byteID, byte[] byteData, 
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local server must be started before data can be send!");
                return;
            }
            
            foreach (var id in clientIDs)
            {
                if (ConnectedClients.ContainsKey(id)) continue;
                _networkManager.Logger?.Log("Client IDs contained a non-existing ID. All client IDs must be valid!");
                return;
            }

            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(DataPacket.PacketType);
            DataPacket packet = new(DataPacket.DataPacketType.Forwarded, 0, false,
                Hashing.GetFNV1aHash32(byteID), byteData);
            DataPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var id in clientIDs)
            {
                _networkManager.Transport?.SendDataToClient(id, data, channel);
            }
        }

        private void ReceiveByteData(uint byteID, byte[] data, uint senderID, ENetworkChannel channel)
        {
            if (!_registeredServerByteDataCallbacks.TryGetValue(byteID, out var callbacks))
                return;

            foreach (var callback in callbacks.Values)
                callback?.Invoke(data, senderID, _networkManager.CurrentTick, DateTime.Now, channel);
        }
        
        #endregion
        
        #region struct data
        
        private readonly ConcurrentDictionary<uint, Dictionary<int, DataPacketCallback>> _registeredServerStructDataCallbacks = new();

        /// <summary>
        /// Registers a callback for a sent struct
        /// </summary>
        /// <param name="callback">Callback which will be invoked after a struct of the same type has been received</param>
        public void RegisterStructData<T>(Action<StructData<T>> callback) where T : struct
        {
	        var structDataHash = Hashing.GetFNV1aHash32(typeof(T).Name);
            
            if (!_registeredServerStructDataCallbacks.TryGetValue(structDataHash, out var callbacks))
			{
                callbacks = new();
                _registeredServerStructDataCallbacks.TryAdd(structDataHash, callbacks);
			}

			var key = callback.GetHashCode();
			var del = CreateStructDataDelegate(callback);
            if (!callbacks.ContainsKey(key))
                callbacks.TryAdd(key, del); 
        }
        
        /// <summary>
        /// Unregisters a callback for a sent struct
        /// </summary>
        /// <param name="callback">Callback which will be invoked after a struct of the same type has been received</param>
        public void UnregisterStructData<T>(Action<StructData<T>> callback) where T : struct
		{
			var structDataHash = Hashing.GetFNV1aHash32(typeof(T).Name);
            
            if (!_registeredServerStructDataCallbacks.TryGetValue(structDataHash, out var callbacks))
                return;

            callbacks.Remove(callback.GetHashCode(), out _);
        }
        
        /// <summary>
        /// Sends a struct from the local server to a given remote client.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
		public void SendStructDataToClient<T>(uint clientID, T structData, 
			ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            SendStructDataToClients(new [] { clientID }, structData, channel); 
        }
        
        /// <summary>
        /// Sends a struct from the local server to all remote clients.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
		public void SendStructDataToAll<T>(T structData, 
			ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            SendStructDataToClients(ConnectedClients.Keys.ToArray(), structData, channel); 
        }
        
        /// <summary>
        /// Sends a struct from the local server to a list of remote clients.
        /// Can only be called after the local server has been started
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
		public void SendStructDataToClients<T>(uint[] clientIDs, T structData, 
			ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local server must be started before data can be send!");
                return;
            }
            
            foreach (var id in clientIDs)
            {
                if (ConnectedClients.ContainsKey(id)) continue;
                _networkManager.Logger?.Log("Client IDs contained a non-existing ID. All client IDs must be valid!");
                return;
            }

            Writer writer = new(_networkManager.SerializerSettings);
            writer.Write(structData);
            var structBuffer = writer.GetBuffer();
            writer.Clear();
            
            writer.WriteByte(DataPacket.PacketType);
            DataPacket packet = new(DataPacket.DataPacketType.Forwarded, 0, true,
                Hashing.GetFNV1aHash32(typeof(T).Name), structBuffer);
            DataPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var id in clientIDs)
            {
                _networkManager.Transport?.SendDataToClient(id, data, channel);
            }
        }
		
		private void ReceiveStructData(uint structHash, byte[] data, uint senderID, ENetworkChannel channel)
		{
			if (!_registeredServerStructDataCallbacks.TryGetValue(structHash, out var callbacks))
				return;

			foreach (var callback in callbacks.Values)
			{
				callback?.Invoke(data, senderID, _networkManager.CurrentTick, DateTime.Now, channel);
			}
        }
        
        #endregion
        
        #region network objects

        private void SpawnNetworkObjects()
        {
            foreach (var nobj in _networkManager.Objects.NetworkObjects)
            {
                if (!nobj.IsSpawned)
                    nobj.Spawn();
            }
        }

        public void SpawnNetworkObject(NetworkObject networkObject, uint authorID = 0)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a network object can be spawned.");
                return;
            }

            if (networkObject == null || networkObject.gameObject.scene.name == null)
            {
                _networkManager.Logger?.LogError("The network object is null or not instantiated yet.");
                return;
            }

            if (networkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network object is already spawned.");
                return;
            }

            networkObject.AuthorID = authorID;
            networkObject.IsAuthor = _networkManager.IsClient && 
                                     _networkManager.Client.ClientID == authorID;
            
            _spawnedNetworkObjects.Add(networkObject.ObjectIdentifier, networkObject);
            networkObject.IsSpawnedServer = true;
            
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(SpawnObjectPacket.PacketType);
            SpawnObjectPacket.Write(writer, SpawnObjectPacket.Build(networkObject));
            var data = writer.GetBuffer();
            foreach (var (clientID, _) in ConnectedClients)
            {
                _networkManager.Transport?.SendDataToClient(clientID, data, ENetworkChannel.ReliableOrdered);
                networkObject.OnRemoteSpawn(clientID);
            }
        }

        public void DespawnNetworkObject(NetworkObject networkObject)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a network object can be despawned.");
                return;
            }
            
            if (networkObject == null || networkObject.gameObject.scene.name == null)
            {
                _networkManager.Logger?.LogError("The network object is null or not instantiated yet.");
                return;
            }
            
            if (!networkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network object is already despawned.");
                return;
            }
            
            foreach (var childNobj in networkObject.gameObject.GetComponentsInChildren<NetworkObject>(true))
            {
                _spawnedNetworkObjects.Remove(childNobj.ObjectIdentifier);
                childNobj.IsSpawnedServer = false;
            }
            
            Writer writer = new(_networkManager.SerializerSettings);
            DespawnObjectPacket packet = new(networkObject.ObjectIdentifier);
            writer.WriteByte(DespawnObjectPacket.PacketType);
            DespawnObjectPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var (clientID, _) in ConnectedClients)
            {
                networkObject.OnRemoteDespawn(clientID);
                _networkManager.Transport?.SendDataToClient(clientID, data, ENetworkChannel.ReliableOrdered);
            }
        }
        
        internal void UpdateNetworkObject(uint clientID, NetworkObject networkObject, UpdateObjectPacket packet)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a network object can be updated.");
                return;
            }
            
            if (networkObject == null || networkObject.gameObject.scene.name == null)
            {
                _networkManager.Logger?.LogError("The network object is null or not instantiated yet.");
                return;
            }
            
            if (!networkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network object must be spawned before it can be updated.");
                return;
            }

            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(UpdateObjectPacket.PacketType);
            UpdateObjectPacket.Write(writer, packet);
            _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        internal void UpdateNetworkObject(NetworkObject networkObject, UpdateObjectPacket packet)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a network object can be updated.");
                return;
            }
            
            if (networkObject == null || networkObject.gameObject.scene.name == null)
            {
                _networkManager.Logger?.LogError("The network object is null or not instantiated yet.");
                return;
            }
            
            if (!networkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network object must be spawned before it can be updated.");
                return;
            }
            
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(UpdateObjectPacket.PacketType);
            UpdateObjectPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var (clientID, _) in ConnectedClients)
                _networkManager.Transport?.SendDataToClient(clientID, data, ENetworkChannel.ReliableOrdered);
        }
        
        internal void SendTransformInitial(uint clientID, NetworkTransform transform, TransformPacket packet, ENetworkChannel networkChannel)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a transform update can be send.");
                return;
            }
            
            if (transform == null || packet == null)
            {
                _networkManager.Logger?.LogError("The network transform is null or not fully defined.");
                return;
            }

            if (!transform.NetworkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network transform must be spawned before it can be updated.");
                return;
            }

            if (!ConnectedClients.ContainsKey(clientID))
            {
                _networkManager.Logger?.LogError("The client ID is invalid for transform initial send.");
                return;
            }
            
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(TransformPacket.PacketType);
            TransformPacket.Write(writer, packet);
            _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), networkChannel);
        }

        internal void SendTransformUpdate(NetworkTransform transform, TransformPacket packet, ENetworkChannel networkChannel)
        {
            if (LocalState != ELocalServerConnectionState.Started)
            {
                _networkManager.Logger?.LogError("The local server has to be started before a transform update can be send.");
                return;
            }
            
            if (transform == null || packet == null)
            {
                _networkManager.Logger?.LogError("The network transform is null or not fully defined.");
                return;
            }

            if (!transform.NetworkObject.IsSpawned)
            {
                _networkManager.Logger?.LogError("The network transform must be spawned before it can be updated.");
                return;
            }

            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(TransformPacket.PacketType);
            TransformPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var (clientID, _) in ConnectedClients)
                _networkManager.Transport?.SendDataToClient(clientID, data, networkChannel);
        }
        
        private void DespawnNetworkObjects()
        {
            foreach (var (_, networkObject) in _spawnedNetworkObjects)
                networkObject.IsSpawnedServer = false;
            _spawnedNetworkObjects.Clear();
        }
        
        #endregion
        
        #region private methods

        private void OnTransportDisposed()
        {
            _authenticatingClients.Clear();
            ConnectedClients.Clear();
            LocalState = ELocalServerConnectionState.Stopped;
        }
        
        private void OnServerStateUpdated(ELocalConnectionState state)
        {
            switch (state)
            {
                case ELocalConnectionState.Starting:
                    _networkManager.Logger?.Log("Server is starting...");
                    break;
                case ELocalConnectionState.Started:
                    ServerEndpoint = _networkManager.Transport.ServerEndpoint;
                    MaxNumberOfClients = _networkManager.Transport.MaxNumberOfClients;
                    _networkManager.Logger?.Log("Server was started");
                    break;
                case ELocalConnectionState.Stopping:
                    _networkManager.Logger?.Log("Server is stopping...");
                    break;
                case ELocalConnectionState.Stopped:
                    ServerEndpoint = null;
                    MaxNumberOfClients = 0;
                    ConnectedClients.Clear();
                    DespawnNetworkObjects();
                    _networkManager.Logger?.Log("Server was stopped");
                    break;
            }
            LocalState = (ELocalServerConnectionState)state;
            OnLocalStateUpdated?.Invoke(LocalState);
            if (state == ELocalConnectionState.Started)
                OnLocalServerStarted?.Invoke();

            if (state == ELocalConnectionState.Started)
                SpawnNetworkObjects();
        }

        private void HandleServernameUpdated()
        {
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ServerUpdatePacket.PacketType);
            ServerUpdatePacket.Write(writer, new(Servername));
            foreach (var id in ConnectedClients.Keys)
                _networkManager.Transport?.SendDataToClient(id, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
            OnServerUpdated?.Invoke();
        }
        
        private void OnRemoteConnectionUpdated(uint clientID, ERemoteConnectionState state)
        {
            switch (state)
            {
                case ERemoteConnectionState.Connected:
                    HandleRemoteClientConnected(clientID);
                    break;
                case ERemoteConnectionState.Disconnected:
                    HandleRemoteClientDisconnected(clientID);
                    break;
            }
        }

        private void HandleRemoteClientConnected(uint clientID)
        {
            if (ConnectedClients.ContainsKey(clientID))
            {
                _networkManager.Logger?.Log($"An already existing connection was overwritten. Connection for client {clientID} was dropped!");
                _networkManager.Transport?.DisconnectClient(clientID);
                return;
            }
            
            // create and save challenge
            Random rnd = new();
            var challenge = (ulong)(rnd.NextDouble() * ulong.MaxValue);
            var hashedChallenge = SHA256.Create().ComputeHash(BitConverter.GetBytes(challenge));
            _authenticatingClients[clientID] = hashedChallenge;

            // send challenge to client
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ConnectionChallengePacket.PacketType);
            ConnectionChallengePacket.Write(writer, new(challenge));
            _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        private void HandleRemoteClientDisconnected(uint clientID)
        {
            // ignore authenticating or missing client IDs
            if (_authenticatingClients.TryRemove(clientID, out _)) return;
            if (!ConnectedClients.TryRemove(clientID, out _)) return;

            // inform other clients of disconnected client
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket.Write(writer, new(clientID));
            var data = writer.GetBuffer();
            foreach (var id in ConnectedClients.Keys)
                _networkManager.Transport?.SendDataToClient(id, data, ENetworkChannel.ReliableOrdered);
            
            _networkManager.Logger?.Log($"Server: Remote client {clientID} was disconnected");
            OnRemoteClientDisconnected?.Invoke(clientID);
        }
        
        private void OnServerReceivedData(ServerReceivedData data)
        {
            try
            {
                Reader reader = new(data.Data, _networkManager.SerializerSettings);
                var packetType = (EPacketType)reader.ReadByte();
                // Debug.Log($"Server Packet: {packetType} from {data.ClientID}");

                switch (packetType)
                {
                    case EPacketType.ChallengeAnswer:
                        HandleChallengeAnswerPacket(data.ClientID, reader);
                        break;
                    case EPacketType.ClientUpdate:
                        HandleClientUpdatePacket(data.ClientID, reader);
                        break;
                    case EPacketType.Data:
                        HandleDataPacket(data.ClientID, reader, data.Channel);
                        break;
                    case EPacketType.UpdateObject:
                        HandleUpdateObjectPacket(data.ClientID, reader);
                        break;
                    case EPacketType.DistributedAuthority:
                        HandleDistributedAuthorityPacket(data.ClientID, reader);
                        break;
                    case EPacketType.Transform:
                        HandleTransformPacket(data.ClientID, reader, data.Channel);
                        break;
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                _networkManager.Logger?.LogError(e.Message);
            }
        }

        private void HandleChallengeAnswerPacket(uint clientID, Reader reader)
        {
            if (!_authenticatingClients.TryGetValue(clientID, out var challenge))
                return;

            var packet = ChallengeAnswerPacket.Read(reader);
            if (!CompareByteArrays(challenge, packet.ChallengeAnswer))
            {
                _networkManager.Transport?.DisconnectClient(clientID);
                _authenticatingClients.TryRemove(clientID, out _);
                return;
            }
            
            // inform client of authentication
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ServerUpdatePacket.PacketType);
            ServerUpdatePacket authentication = new(clientID, Servername, MaxNumberOfClients);
            ServerUpdatePacket.Write(writer, authentication);
            _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
            writer.Clear();
            
            // inform client of other clients
            writer.WriteByte(ClientUpdatePacket.PacketType);
            var pos = writer.Position;
            foreach (var (_, clientInfo) in ConnectedClients)
            {
                writer.Position = pos;
                ClientUpdatePacket existingClient = new(clientInfo.ID, ClientUpdatePacket.UpdateType.Connected, clientInfo.Username, clientInfo.UserColour);
                ClientUpdatePacket.Write(writer, existingClient);
                _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
            }
            writer.Clear();
            
            // inform other clients of new client
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket update = new(clientID, ClientUpdatePacket.UpdateType.Connected, packet.Username, packet.Colour);
            ClientUpdatePacket.Write(writer, update);
            var data = writer.GetBuffer();
            foreach (var id in ConnectedClients.Keys)
                _networkManager.Transport?.SendDataToClient(id, data, ENetworkChannel.ReliableOrdered);
            writer.Clear();
            
            // authenticate client
            ConnectedClients[clientID] = new(clientID, packet.Username, packet.Colour);
            _authenticatingClients.TryRemove(clientID, out _);
            
            // replicate current network objects on client
            var sentObjects = new HashSet<uint>();
            foreach (var networkObject in _spawnedNetworkObjects.Values.ToList())
                SendSpawnedNetworkObject(clientID, networkObject, writer, sentObjects);
            
            _networkManager.Logger?.Log($"Server: Remote client {clientID} was connected");
            OnRemoteClientConnected?.Invoke(clientID);
        }

        private void SendSpawnedNetworkObject(uint clientID, NetworkObject networkObject, Writer writer, HashSet<uint> sentObjects)
        {
            // make sure all parents are sent first
            if (networkObject.ParentIdentifier != null && !sentObjects.Contains((uint)networkObject.ParentIdentifier))
                SendSpawnedNetworkObject(clientID, networkObject.Parent, writer, sentObjects);
            
            writer.WriteByte(SpawnObjectPacket.PacketType);
            SpawnObjectPacket.Write(writer, SpawnObjectPacket.Build(networkObject));
            _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
            writer.Clear();
            
            sentObjects.Add(networkObject.ObjectIdentifier);
            networkObject.OnRemoteSpawn(clientID);
        }

        private void HandleClientUpdatePacket(uint clientID, Reader reader)
        {
            if (!ConnectedClients.ContainsKey(clientID))
                return;

            var packet = ClientUpdatePacket.Read(reader);
            if (packet.ClientID != clientID || packet.Type != ClientUpdatePacket.UpdateType.Updated)
                return;
            
            // apply update
            if (packet.Username is not null)
                ConnectedClients[clientID].Username = packet.Username;
            if (packet.Colour is not null)
                ConnectedClients[clientID].UserColour = (Color32)packet.Colour;

            // inform other clients of update
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var id in ConnectedClients.Keys)
            {
                if (id == clientID) continue;
                _networkManager.Transport?.SendDataToClient(id, data, ENetworkChannel.ReliableOrdered);
            }
            
            OnRemoteClientUpdated?.Invoke(clientID);
        }

        private void HandleDataPacket(uint clientID, Reader reader, ENetworkChannel channel)
        {
            if (!ConnectedClients.ContainsKey(clientID))
                return;

            var packet = DataPacket.Read(reader);
            uint[] targetIDs = { };
            switch (packet.DataType)
            {
                case DataPacket.DataPacketType.ToServer:
                    if (packet.IsStructData)
                        // ReSharper disable once PossibleInvalidOperationException
                        ReceiveStructData(packet.DataID, packet.Data, clientID, channel);
                    else
                        // ReSharper disable once PossibleInvalidOperationException
                        ReceiveByteData(packet.DataID, packet.Data, clientID, channel);
                    return;
                case DataPacket.DataPacketType.Forwarded:
                    return;
                case DataPacket.DataPacketType.ToClient:
                    // ReSharper disable once PossibleInvalidOperationException
                    targetIDs = new[] { (uint)packet.TargetID };
                    break;
                case DataPacket.DataPacketType.ToClients:
                    targetIDs = packet.TargetIDs;
                    break;
            }
            
            // forward data to defined clients
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(DataPacket.PacketType);
            DataPacket forwardedPacket = new(DataPacket.DataPacketType.Forwarded, clientID, packet.IsStructData,
                packet.DataID, packet.Data);
            DataPacket.Write(writer, forwardedPacket);
            var data = writer.GetBuffer();
            foreach (var id in targetIDs)
            {
                if (id == clientID) continue;
                _networkManager.Transport?.SendDataToClient(id, data, channel);
            }
        }

        private void HandleUpdateObjectPacket(uint clientID, Reader reader)
        {
            if (!ConnectedClients.ContainsKey(clientID))
                return;
            
            var packet = UpdateObjectPacket.Read(reader);

            if (!_spawnedNetworkObjects.TryGetValue(packet.ObjectIdentifier, out var networkObject))
            {
                _networkManager.Logger?.LogError("Received an invalid identifier for updating a network object.");
                return;
            }
            
            Writer writer = new(_networkManager.SerializerSettings);

            if (!networkObject.DistributedAuthority) return; // ignore client side updates
            if (networkObject.AuthorID != clientID)
            {   // inform client they dont have authority
                var authPacket = new UpdateObjectPacket.Builder(networkObject.ObjectIdentifier)
                    .WithAuthorityUpdate(networkObject.AuthorID, networkObject.AuthoritySequence, networkObject.OwnerID, networkObject.OwnershipSequence)
                    .Build();
                writer.WriteByte(UpdateObjectPacket.PacketType);
                UpdateObjectPacket.Write(writer, authPacket);
                _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
                return;
            }
            
            if (packet.Flags.HasFlag(UpdateObjectPacket.EFlags.Parent))
            {
                if (packet.ParentIdentifier == null)
                    networkObject.transform.parent = null;
                else if (!_spawnedNetworkObjects.TryGetValue((uint)packet.ParentIdentifier, out var parentObject))
                    _networkManager.Logger?.LogError("Received a parent identifier for spawning with an unspawned parent.");
                else
                    networkObject.transform.parent = parentObject.transform;
            }
            if (packet.Flags.HasFlag(UpdateObjectPacket.EFlags.Active))
            {
                networkObject.gameObject.SetActive(packet.IsActive);
            }
            
            // forward update to other clients
            writer.WriteByte(UpdateObjectPacket.PacketType);
            UpdateObjectPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var (key, _) in ConnectedClients)
            {
                if (key == clientID) continue;
                _networkManager.Transport?.SendDataToClient(key, data, ENetworkChannel.ReliableOrdered);
            }
        }

        private void HandleDistributedAuthorityPacket(uint clientID, Reader reader)
        {
            if (!ConnectedClients.ContainsKey(clientID))
                return;

            var packet = DistributedAuthorityPacket.Read(reader);
            if (!_spawnedNetworkObjects.TryGetValue(packet.ObjectIdentifier, out var networkObject))
            {
                _networkManager.Logger?.LogError("Received an invalid object identifier for updating distributed authority on an unspawned object.");
                return;
            }
            
            networkObject.UpdateDistributedAuthorityServer(clientID, packet);
        }

        private void HandleTransformPacket(uint clientID, Reader reader, ENetworkChannel channel)
        {
            if (!ConnectedClients.ContainsKey(clientID))
                return;
            
            var packet = TransformPacket.Read(reader);
            if (!_spawnedNetworkObjects.TryGetValue(packet.ObjectIdentifier, out var networkObject))
            {
                _networkManager.Logger?.LogError("Received an invalid object identifier for a transform update.");
                return;
            }

            if (!networkObject.TryGetComponent<NetworkTransform>(out var transform))
            {
                _networkManager.Logger?.LogError("Received a transform update for a non-transform network object.");
                return;
            }
            
            Writer writer = new(_networkManager.SerializerSettings);
            
            if (!networkObject.DistributedAuthority) return; // ignore client side updates
            if (networkObject.AuthorID != clientID)
            {   // inform client they dont have authority
                var authPacket = new UpdateObjectPacket.Builder(networkObject.ObjectIdentifier)
                    .WithAuthorityUpdate(networkObject.AuthorID, networkObject.AuthoritySequence, networkObject.OwnerID, networkObject.OwnershipSequence)
                    .Build();
                writer.WriteByte(UpdateObjectPacket.PacketType);
                UpdateObjectPacket.Write(writer, authPacket);
                _networkManager.Transport?.SendDataToClient(clientID, writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
                return;
            }

            transform.ReceiveTransformUpdate(packet, _networkManager.CurrentTick, DateTime.Now);
            
            // forward update to other clients
            writer.WriteByte(TransformPacket.PacketType);
            TransformPacket.Write(writer, packet);
            var data = writer.GetBuffer();
            foreach (var (key, _) in ConnectedClients)
            {
                if (key == clientID) continue;
                _networkManager.Transport?.SendDataToClient(key, data, channel);
            }
        }
        
        private static bool CompareByteArrays(IEnumerable<byte> a, IEnumerable<byte> b)
        {
            return a.SequenceEqual(b);
        }
        
        #endregion
        
        #region utilities
        
        private delegate void DataPacketCallback(byte[] data, uint senderID, uint tick, DateTime timestamp, ENetworkChannel channel);
        
        private DataPacketCallback CreateByteDataDelegate(Action<ByteData> callback)
        {
            return ParseDelegate;
            void ParseDelegate(byte[] data, uint senderID, uint tick, DateTime timestamp, ENetworkChannel channel)
            {
                callback?.Invoke(new()
                {
                    Data = data,
                    SenderID = senderID,
                    Tick = tick,
                    Timestamp = timestamp,
                    Channel = channel
                });
            }
        }
        
        private DataPacketCallback CreateStructDataDelegate<T>(Action<StructData<T>> callback)
        {
            return ParseDelegate;
            void ParseDelegate(byte[] data, uint senderID, uint tick, DateTime timestamp, ENetworkChannel channel)
            {
                Reader reader = new(data, _networkManager.SerializerSettings);
                callback?.Invoke(new()
                {
                    Data = reader.Read<T>(),
                    SenderID = senderID,
                    Tick = tick,
                    Timestamp = timestamp,
                    Channel = channel
                });
            }
        }
        
        #endregion
    }
    
    public enum ELocalServerConnectionState
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
}
