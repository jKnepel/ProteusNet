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

namespace jKnepel.ProteusNet.Networking
{
    [Serializable]
    public class Client
    {
        #region fields

        /// <summary>
        /// Whether the local server has been started or not
        /// </summary>
        public bool IsActive => LocalState == ELocalClientConnectionState.Authenticated;
        
        /// <summary>
        /// Endpoint of the server to which the local client is connected
        /// </summary>
        public IPEndPoint ServerEndpoint { get; private set; }
        /// <summary>
        /// Name of the server to which the local client is connected
        /// </summary>
        public string Servername { get; private set; }
        /// <summary>
        /// Max number of connected clients of the server to which the local client is connected
        /// </summary>
        public uint MaxNumberOfClients { get; private set; }
        
        /// <summary>
        /// Identifier of the local client
        /// </summary>
        public uint ClientID { get; private set; }
        /// <summary>
        /// Username of the local client
        /// </summary>
        public string Username
        {
            get => username;
            set
            {
                if (value is null || value.Equals(username)) return;
                username = value;
                if (IsActive)
                    HandleUsernameUpdate();
            }
        }
        /// <summary>
        /// UserColour of the local client
        /// </summary>
        public Color32 UserColour
        {
            get => userColour;
            set
            {
                if (value.Equals(userColour)) return;
                userColour = value;
                if (IsActive)
                    HandleColourUpdate();
            }
        }
        /// <summary>
        /// The current connection state of the local client
        /// </summary>
        public ELocalClientConnectionState LocalState { get; private set; } = ELocalClientConnectionState.Stopped;
        /// <summary>
        /// The remote clients that are connected to the same server
        /// </summary>
        public ConcurrentDictionary<uint, ClientInformation> ConnectedClients { get; } = new();
        /// <summary>
        /// The number of clients connected to the same server
        /// </summary>
        public uint NumberOfConnectedClients => (uint)(IsActive ? ConnectedClients.Count + 1 : 0);
        
        /// <summary>
        /// Called when the local client's connection state has been updated
        /// </summary>
        public event Action<ELocalClientConnectionState> OnLocalStateUpdated;
        /// <summary>
        /// Called when the local client's connection state was started and authenticated
        /// </summary>
        public event Action OnLocalClientStarted;
        /// <summary>
        /// Called by the local client when a new remote client has been authenticated
        /// </summary>
        public event Action<uint> OnRemoteClientConnected;
        /// <summary>
        /// Called by the local client when a remote client disconnected
        /// </summary>
        public event Action<uint> OnRemoteClientDisconnected;
        /// <summary>
        /// Called by the local client when a remote client updated its information
        /// </summary>
        public event Action<uint> OnRemoteClientUpdated;
        /// <summary>
        /// Called by the local client when the remote server updated its information
        /// </summary>
        public event Action OnServerUpdated;
        
        private readonly Dictionary<uint, NetworkObject> _spawnedNetworkObjects = new();
        
        private INetworkManager _networkManager;
        
        [SerializeField] private string username = "Username";
        [SerializeField] private Color32 userColour = new(153, 191, 97, 255);
        
        #endregion
        
        #region lifecycle

        internal void Initialize(INetworkManager networkManager)
        {
            _networkManager = networkManager;
            _networkManager.OnTransportExchanged += Reset;
            Reset();
        }

        private void Reset()
        {
            ConnectedClients.Clear();
            LocalState = ELocalClientConnectionState.Stopped;
            
            if (_networkManager.Transport == null) return;
            _networkManager.Transport.OnClientStateUpdated += OnClientStateUpdated;
            _networkManager.Transport.OnClientReceivedData += OnClientReceivedData;
        }

        public void Start()
        {
            if (_networkManager == null)
            {
                Debug.LogError("The client has to be initialized before it can be started!");
                return;
            }
            
            _networkManager.StartClient();
        }

        public void Stop()
        {
            if (_networkManager == null)
            {
                Debug.LogError("The client has to be initialized before it can be stopped!");
                return;
            }
            
            _networkManager.StopClient();
        }
        
        #endregion
        
        #region byte data
        
        private readonly ConcurrentDictionary<uint, Dictionary<int, DataPacketCallback>> _registeredClientByteDataCallbacks = new();

        /// <summary>
        /// Registers a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">Callback which will be invoked after byte data with the given id has been received</param>
        public void RegisterByteData(string byteID, Action<ByteData> callback)
        {
            var byteDataHash = Hashing.GetFNV1aHash32(byteID);

            if (!_registeredClientByteDataCallbacks.TryGetValue(byteDataHash, out var callbacks))
            {
                callbacks = new();
                _registeredClientByteDataCallbacks.TryAdd(byteDataHash, callbacks);
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

            if (!_registeredClientByteDataCallbacks.TryGetValue(byteDataHash, out var callbacks))
                return;

            callbacks.Remove(callback.GetHashCode(), out _);
        }
        
        /// <summary>
        /// Sends byte data with a given id from the local client to the server.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        public void SendByteDataToServer(string byteID, byte[] byteData, 
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered)
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local client must be started before data can be send!");
                return;
            }

            Writer writer = new();
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(false, Hashing.GetFNV1aHash32(byteID), byteData);
            DataPacket.Write(writer, dataPacket);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), channel);
        }
        
        /// <summary>
        /// Sends byte data with a given id from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
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
        /// Sends byte data with a given id from the local client to all other remote clients.
        /// Can only be called after the local client has been authenticated
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
        /// Sends byte data with a given id from the local client to a list of remote clients.
        /// Can only be called after the local client has been authenticated
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
                _networkManager.Logger?.Log("The local client must be started before data can be send!");
                return;
            }
            
            foreach (var id in clientIDs)
            {
                if (ConnectedClients.ContainsKey(id)) continue;
                _networkManager.Logger?.Log("Client IDs contained a non-existing ID. All client IDs must be valid!");
                return;
            }

            Writer writer = new();
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(clientIDs, false, Hashing.GetFNV1aHash32(byteID), byteData);
            DataPacket.Write(writer, dataPacket);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), channel);
        }
 
        private void ReceiveByteData(uint byteID, byte[] data, uint senderID, ENetworkChannel channel)
        {
            if (!_registeredClientByteDataCallbacks.TryGetValue(byteID, out var callbacks))
                return;

            foreach (var callback in callbacks.Values)
                callback?.Invoke(data, senderID, _networkManager.CurrentTick, DateTime.Now, channel);
        }
        
        #endregion
        
        #region struct data
        
        private readonly ConcurrentDictionary<uint, Dictionary<int, DataPacketCallback>> _registeredClientStructDataCallbacks = new();

        /// <summary>
        /// Registers a callback for a sent struct
        /// </summary>
        /// <param name="callback">Callback which will be invoked after a struct of the same type has been received</param>
        public void RegisterStructData<T>(Action<StructData<T>> callback) where T : struct
        {
	        var structDataHash = Hashing.GetFNV1aHash32(typeof(T).Name);
            
            if (!_registeredClientStructDataCallbacks.TryGetValue(structDataHash, out var callbacks))
			{
                callbacks = new();
                _registeredClientStructDataCallbacks.TryAdd(structDataHash, callbacks);
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
            
            if (!_registeredClientStructDataCallbacks.TryGetValue(structDataHash, out var callbacks))
                return;

            callbacks.Remove(callback.GetHashCode(), out _);
        }
        
        /// <summary>
        /// Sends a struct from the local client to the server.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        public void SendStructDataToServer<T>(T structData, 
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local client must be started before data can be send!");
                return;
            }

            Writer writer = new();
            writer.Write(structData);
            var structBuffer = writer.GetBuffer();
            writer.Clear();
            
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(true, Hashing.GetFNV1aHash32(typeof(T).Name), structBuffer);
            DataPacket.Write(writer, dataPacket);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), channel);
        }
        
        /// <summary>
        /// Sends a struct from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
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
        /// Sends a struct from the local client to all other remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
		public void SendStructDataToAll<T>(T structData, 
			ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            SendStructDataToClients(ConnectedClients.Keys.ToArray(), structData, channel); 
        }

        /// <summary>
        /// Sends a struct from the local client to a list of other remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
		public void SendStructDataToClients<T>(uint[] clientIDs, T structData, 
			ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct 
        {
            if (!IsActive)
            {
                _networkManager.Logger?.Log("The local client must be started before data can be send!");
                return;
            }
            
            foreach (var id in clientIDs)
            {
                if (ConnectedClients.ContainsKey(id)) continue;
                _networkManager.Logger?.Log("Client IDs contained a non-existing ID. All client IDs must be valid!");
                return;
            }

            Writer writer = new();
            writer.Write(structData);
            var structBuffer = writer.GetBuffer();
            writer.Clear();
            
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(clientIDs, true, Hashing.GetFNV1aHash32(typeof(T).Name), structBuffer);
            DataPacket.Write(writer, dataPacket);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), channel); 
        }
		
		private void ReceiveStructData(uint structHash, byte[] data, uint senderID, ENetworkChannel channel)
		{
			if (!_registeredClientStructDataCallbacks.TryGetValue(structHash, out var callbacks))
				return;

			foreach (var callback in callbacks.Values)
				callback?.Invoke(data, senderID, _networkManager.CurrentTick, DateTime.Now, channel);
        }
        
        #endregion
        
        #region network objects
        
        internal void UpdateNetworkObject(NetworkObject networkObject, UpdateObjectPacket packet)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
            {
                _networkManager.Logger?.LogError("The local client has to be started before a network object can be updated.");
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

            if (!networkObject.IsAuthor)
            {
                _networkManager.Logger?.LogError("The local client must have authority before the object can be updated.");
                return;
            }
            
            Writer writer = new();
            writer.WriteByte(UpdateObjectPacket.PacketType);
            UpdateObjectPacket.Write(writer, packet);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        internal void UpdateDistributedAuthority(NetworkObject networkObject, DistributedAuthorityPacket.EType type, ushort authoritySequence, ushort ownershipSequence)
        {
            DistributedAuthorityPacket packet = new(networkObject.ObjectIdentifier, type, authoritySequence, ownershipSequence);
            
            Writer writer = new();
            writer.WriteByte(DistributedAuthorityPacket.PacketType);
            DistributedAuthorityPacket.Write(writer, packet);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }
        
        internal void SendTransformUpdate(NetworkTransform transform, TransformPacket packet, ENetworkChannel networkChannel)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
            {
                _networkManager.Logger?.LogError("The local client has to be started before a transform update can be send.");
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

            if (!transform.NetworkObject.IsAuthor)
            {
                _networkManager.Logger?.LogError("The local client must have authority before the object can be updated.");
                return;
            }

            Writer writer = new();
            writer.WriteByte(TransformPacket.PacketType);
            TransformPacket.Write(writer, packet);
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), networkChannel);
        }
        
        #endregion

        #region private methods
        
        private void OnClientStateUpdated(ELocalConnectionState state)
        {
            switch (state)
            {
                case ELocalConnectionState.Starting:
                    _networkManager.Logger?.Log("Client is starting...");
                    break;
                case ELocalConnectionState.Started:
                    _networkManager.Logger?.Log("Client was started");
                    break;
                case ELocalConnectionState.Stopping:
                    _networkManager.Logger?.Log("Client is stopping...");
                    break;
                case ELocalConnectionState.Stopped:
                    ServerEndpoint = null;
                    MaxNumberOfClients = 0;
                    Servername = string.Empty;
                    ClientID = 0;
                    ConnectedClients.Clear();
                    DespawnNetworkObjects();
                    _networkManager.Logger?.Log("Client was stopped");
                    break;
            }
            LocalState = (ELocalClientConnectionState)state;
            OnLocalStateUpdated?.Invoke(LocalState);
        }
        
        private void OnClientReceivedData(ClientReceivedData data)
        {
            try
            {
                Reader reader = new(data.Data);
                var packetType = (EPacketType)reader.ReadByte();
                // Debug.Log($"Client Packet: {packetType}");

                switch (packetType)
                {
                    case EPacketType.ConnectionChallenge:
                        HandleConnectionChallengePacket(reader);
                        break;
                    case EPacketType.ServerUpdate:
                        HandleServerUpdatePacket(reader);
                        break;
                    case EPacketType.ClientUpdate:
                        HandleClientUpdatePacket(reader);
                        break;
                    case EPacketType.Data:
                        HandleDataPacket(reader, data.Channel);
                        break;
                    case EPacketType.SpawnObject:
                        HandleSpawnObjectPacket(reader);
                        break;
                    case EPacketType.UpdateObject:
                        HandleUpdateObjectPacket(reader);
                        break;
                    case EPacketType.DespawnObject:
                        HandleDespawnObjectPacket(reader);
                        break;
                    case EPacketType.Transform:
                        HandleTransformPacket(reader);
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

        private void HandleUsernameUpdate()
        {
            Writer writer = new();
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket.Write(writer, new(ClientID, ClientUpdatePacket.UpdateType.Updated, Username, null));
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        private void HandleColourUpdate()
        {
            Writer writer = new();
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket.Write(writer, new(ClientID, ClientUpdatePacket.UpdateType.Updated, null, UserColour));
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        private void HandleConnectionChallengePacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Started)
                return;
            
            var packet = ConnectionChallengePacket.Read(reader);
            var hashedChallenge = SHA256.Create().ComputeHash(BitConverter.GetBytes(packet.Challenge));
            
            Writer writer = new();
            writer.WriteByte(ChallengeAnswerPacket.PacketType);
            ChallengeAnswerPacket.Write(writer, new(hashedChallenge, Username, UserColour));
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }
        
        private void HandleServerUpdatePacket(Reader reader)
        {
            var packet = ServerUpdatePacket.Read(reader);

            switch (packet.Type)
            {
                case ServerUpdatePacket.UpdateType.Authenticated:
                    if (LocalState != ELocalClientConnectionState.Started)
                        return;
                    
                    if (packet.ClientID is null || packet.Servername is null || packet.MaxNumberConnectedClients is null)
                        throw new NullReferenceException("Invalid server update packet values received");
                    
                    ServerEndpoint = _networkManager.Transport.ServerEndpoint;
                    MaxNumberOfClients = (uint)packet.MaxNumberConnectedClients;
                    Servername = packet.Servername;
                    ClientID = (uint)packet.ClientID;
                    _networkManager.Logger?.Log("Client was authenticated");
                    LocalState = ELocalClientConnectionState.Authenticated;
                    OnLocalStateUpdated?.Invoke(LocalState);
                    OnLocalClientStarted?.Invoke();
                    break;
                case ServerUpdatePacket.UpdateType.Updated:
                    if (LocalState != ELocalClientConnectionState.Authenticated)
                        return;

                    Servername = packet.Servername ?? throw new NullReferenceException("Invalid server update packet values received");
                    OnServerUpdated?.Invoke();
                    break;
            }
        }

        private void HandleClientUpdatePacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;

            var packet = ClientUpdatePacket.Read(reader);
            var clientID = packet.ClientID;
            switch (packet.Type)
            {
                case ClientUpdatePacket.UpdateType.Connected:
                    if (packet.Username is null || packet.Colour is null)
                        throw new NullReferenceException("Client connection update packet contained invalid values!");
                    ConnectedClients[clientID] = new(clientID, packet.Username, (Color32)packet.Colour);
                    _networkManager.Logger?.Log($"Client: Remote client {clientID} was connected");
                    OnRemoteClientConnected?.Invoke(clientID);
                    break;
                case ClientUpdatePacket.UpdateType.Disconnected:
                    if (!ConnectedClients.TryRemove(clientID, out _)) return;
                    _networkManager.Logger?.Log($"Client: Remote client {clientID} was disconnected");
                    OnRemoteClientDisconnected?.Invoke(clientID);
                    break;
                case ClientUpdatePacket.UpdateType.Updated:
                    if (packet.Username is not null)
                        ConnectedClients[clientID].Username = packet.Username;
                    if (packet.Colour is not null)
                        ConnectedClients[clientID].UserColour = (Color32)packet.Colour;
                    OnRemoteClientUpdated?.Invoke(clientID);
                    break;
            }
        }
        
        private void HandleDataPacket(Reader reader, ENetworkChannel channel)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;

            var packet = DataPacket.Read(reader);
            if (packet.DataType != DataPacket.DataPacketType.Forwarded)
                return;
            
            if (packet.IsStructData)
                ReceiveStructData(packet.DataID, packet.Data, (uint)packet.SenderID, channel);
            else
                ReceiveByteData(packet.DataID, packet.Data, (uint)packet.SenderID, channel);
        }

        private void HandleSpawnObjectPacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;
            
            var packet = SpawnObjectPacket.Read(reader);
            
            Transform parent = null;
            if (packet.Flags.HasFlag(SpawnObjectPacket.EFlags.HasParent))
            {
                if (!_spawnedNetworkObjects.TryGetValue(packet.ParentIdentifier, out var parentObject))
                    _networkManager.Logger?.LogError("Received a parent identifier for spawning of an unspawned parent.");
                else
                    parent = parentObject.transform;
            }

            if (_networkManager.IsServer)
            {   // dont spawn object again on host client
                if (!_networkManager.Objects.TryGetValue(packet.ObjectIdentifier, out var localObject))
                {
                    _networkManager.Logger?.LogError("Received invalid identifier for spawning an object on the host-client.");
                    return;
                }
                
                _spawnedNetworkObjects.Add(localObject.ObjectIdentifier, localObject);
                localObject.IsSpawnedClient = true;
                return;
            }

            NetworkObject networkObject;
            if (packet.Flags.HasFlag(SpawnObjectPacket.EFlags.Placed))
            {   // only retrieve already placed object
                if (!_networkManager.Objects.TryGetValue(packet.ObjectIdentifier, out networkObject))
                {
                    _networkManager.Logger?.LogError("Received invalid identifier for spawning a placed object.");
                    return;
                }
            }
            else
            {   // spawn and initialize new object
                if (!_networkManager.NetworkObjectPrefabs.TryGet((int)packet.PrefabIdentifier, out var prefab))
                {
                    _networkManager.Logger?.LogError("Received invalid prefab identifier for spawning an instantiated object.");
                    return;
                }

                // ensures correct identifier for registering after instantiation
                var tmpID = prefab.ObjectIdentifier;
                prefab.ObjectIdentifier = packet.ObjectIdentifier;
                networkObject = GameObject.Instantiate(prefab);
                prefab.ObjectIdentifier = tmpID;
                    
                networkObject.ObjectType = EObjectType.Instantiated;
                networkObject.ObjectIdentifier = packet.ObjectIdentifier;
            }
            
            networkObject.transform.parent = parent;
            networkObject.gameObject.SetActive(packet.IsActive);
            networkObject.AuthorID = packet.AuthorID;
            networkObject.AuthoritySequence = packet.AuthorSequence;
            networkObject.IsAuthor = packet.AuthorID == ClientID;
            networkObject.OwnerID = packet.OwnerID;
            networkObject.OwnershipSequence = packet.OwnerSequence;
            networkObject.IsOwner = packet.OwnerID == ClientID;
            networkObject.ShouldReplicate = networkObject.DistributedAuthority 
                ? networkObject.IsAuthor || networkObject.AuthorID == 0 && _networkManager.IsServer 
                : _networkManager.IsServer;
            
            _spawnedNetworkObjects.Add(networkObject.ObjectIdentifier, networkObject);
            networkObject.IsSpawnedClient = true;
        }
        
        private void HandleUpdateObjectPacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;

            if (_networkManager.IsServer)
                return;

            var packet = UpdateObjectPacket.Read(reader);

            if (!_spawnedNetworkObjects.TryGetValue(packet.ObjectIdentifier, out var networkObject))
            {
                _networkManager.Logger?.LogError("Received an invalid identifier for updating a network object.");
                return;
            }
            
            if (networkObject.ShouldReplicate)
                return;

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

            if (packet.Flags.HasFlag(UpdateObjectPacket.EFlags.Authority))
            {
                networkObject.UpdateDistributedAuthorityClient(packet.AuthorID, packet.AuthoritySequence, packet.OwnerID, packet.OwnershipSequence);
            }
        }

        private void HandleDespawnObjectPacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;

            var packet = DespawnObjectPacket.Read(reader);
            if (!_spawnedNetworkObjects.TryGetValue(packet.ObjectIdentifier, out var networkObject))
            {
                _networkManager.Logger?.LogError("Received an invalid object identifier for despawning.");
                return;
            }

            foreach (var childNobj in networkObject.gameObject.GetComponentsInChildren<NetworkObject>(true))
            {
                _spawnedNetworkObjects.Remove(childNobj.ObjectIdentifier);
                childNobj.IsSpawnedClient = false;
            }
        }

        private void HandleTransformPacket(Reader reader)
        {
            if (LocalState != ELocalClientConnectionState.Authenticated)
                return;

            if (_networkManager.IsServer)
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

            if (packet.IsInitialTransform)
            {
                var trf = networkObject.transform;
                var localPosition = trf.localPosition;
                var localRotation = trf.localEulerAngles;
                var localScale = trf.localScale;
                
                var position = new Vector3(
                    packet.PositionX ?? localPosition.x,
                    packet.PositionY ?? localPosition.y,
                    packet.PositionZ ?? localPosition.z
                );
                var rotation = new Vector3(
                    packet.RotationX ?? localRotation.x,
                    packet.RotationY ?? localRotation.y,
                    packet.RotationZ ?? localRotation.z
                );
                var scale = new Vector3(
                    packet.ScaleX ?? localScale.x,
                    packet.ScaleY ?? localScale.y,
                    packet.ScaleZ ?? localScale.z
                );
                
                trf.SetPositionAndRotation(position, Quaternion.Euler(rotation));
                trf.localScale = scale;
                return;
            }
            
            if (networkObject.ShouldReplicate)
                return;

            transform.ReceiveTransformUpdate(packet, _networkManager.CurrentTick, DateTime.Now);
        }

        private void DespawnNetworkObjects()
        {
            foreach (var (_, networkObject) in _spawnedNetworkObjects)
                networkObject.IsSpawnedClient = false;
            _spawnedNetworkObjects.Clear();
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
                Reader reader = new(data);
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

    public enum ELocalClientConnectionState
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
        Stopped = 3,
        /// <summary>
        /// Signifies that an established local connection has been authenticated and is ready to send data
        /// </summary>
        Authenticated = 4,
    }
}
