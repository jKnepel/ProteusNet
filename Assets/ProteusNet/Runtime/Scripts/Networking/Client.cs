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
            get => _username;
            set
            {
                if (value is null || value.Equals(_username)) return;
                _username = value;
                if (IsActive)
                    HandleUsernameUpdate();
            }
        }
        /// <summary>
        /// UserColour of the local client
        /// </summary>
        public Color32 UserColour
        {
            get => _userColour;
            set
            {
                if (value.Equals(_userColour)) return;
                _userColour = value;
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

        private readonly NetworkManager _networkManager;
        private string _username = "Username";
        private Color32 _userColour = new(153, 191, 97, 255);
        
        #endregion

        public Client(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _networkManager.OnTransportDisposed += OnTransportDisposed;
            _networkManager.OnClientStateUpdated += OnClientStateUpdated;
            _networkManager.OnClientReceivedData += OnClientReceivedData;
        }
        
        #region byte data
        
        private readonly ConcurrentDictionary<uint, Dictionary<int, DataPacketCallback>> _registeredClientByteDataCallbacks = new();

        /// <summary>
        /// Registers a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">Callback which will be invoked after byte data with the given id has been received</param>
        public void RegisterByteData(string byteID, Action<ByteData> callback)
        {
            var byteDataHash = Hashing.GetFNV1Hash32(byteID);

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
            var byteDataHash = Hashing.GetFNV1Hash32(byteID);

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

            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(false, Hashing.GetFNV1Hash32(byteID), byteData);
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

            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(clientIDs, false, Hashing.GetFNV1Hash32(byteID), byteData);
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
	        var structDataHash = Hashing.GetFNV1Hash32(typeof(T).Name);
            
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
			var structDataHash = Hashing.GetFNV1Hash32(typeof(T).Name);
            
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

            Writer writer = new(_networkManager.SerializerSettings);
            writer.Write(structData);
            var structBuffer = writer.GetBuffer();
            writer.Clear();
            
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(true, Hashing.GetFNV1Hash32(typeof(T).Name), structBuffer);
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

            Writer writer = new(_networkManager.SerializerSettings);
            writer.Write(structData);
            var structBuffer = writer.GetBuffer();
            writer.Clear();
            
            writer.WriteByte(DataPacket.PacketType);
            DataPacket dataPacket = new(clientIDs, true, Hashing.GetFNV1Hash32(typeof(T).Name), structBuffer);
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

        #region private methods

        private void OnTransportDisposed()
        {
            ConnectedClients.Clear();
            LocalState = ELocalClientConnectionState.Stopped;
        }
        
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
                Reader reader = new(data.Data, _networkManager.SerializerSettings);
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
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                _networkManager.Logger?.Log(e.Message);
            }
        }

        private void HandleUsernameUpdate()
        {
            Writer writer = new(_networkManager.SerializerSettings);
            writer.WriteByte(ClientUpdatePacket.PacketType);
            ClientUpdatePacket.Write(writer, new(ClientID, ClientUpdatePacket.UpdateType.Updated, Username, null));
            _networkManager.Transport?.SendDataToServer(writer.GetBuffer(), ENetworkChannel.ReliableOrdered);
        }

        private void HandleColourUpdate()
        {
            Writer writer = new(_networkManager.SerializerSettings);
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
            
            Writer writer = new(_networkManager.SerializerSettings);
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
                    LocalState = ELocalClientConnectionState.Authenticated;
                    OnLocalStateUpdated?.Invoke(LocalState);
                    _networkManager.Logger?.Log("Client was authenticated");
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
                // ReSharper disable once PossibleInvalidOperationException
                ReceiveStructData(packet.DataID, packet.Data, (uint)packet.SenderID, channel);
            else
                // ReSharper disable once PossibleInvalidOperationException
                ReceiveByteData(packet.DataID, packet.Data, (uint)packet.SenderID, channel);
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
