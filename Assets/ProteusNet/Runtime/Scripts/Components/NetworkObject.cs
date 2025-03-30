using jKnepel.ProteusNet.Networking.Packets;
using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using jKnepel.ProteusNet.Utilities;
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components
{
    public enum EObjectType
    {
        /// <summary>
        /// NetworkObjects that exist as serialized asset outside of a scene.
        /// </summary>
        Asset,
        /// <summary>
        /// NetworkObjects which are placed in-scene during the editing of a scene.
        /// </summary>
        Placed,
        /// <summary>
        /// NetworkObjects which are instantiated during the playmode.
        /// </summary>
        Instantiated
    }
    
    [DefaultExecutionOrder(-2)]
    [DisallowMultipleComponent]
    [AddComponentMenu("ProteusNet/Network Object")]
    public class NetworkObject : MonoBehaviour, IEquatable<NetworkObject>
    {
        #region attributes
        
        [SerializeField] private MonoNetworkManager networkManager;
        public MonoNetworkManager NetworkManager
        {
            get => networkManager;
            set
            {
                if (value == networkManager || IsSpawned) return;
                networkManager = value;
            }
        }

        [SerializeField] private EObjectType objectType;
        public EObjectType ObjectType
        {
            get => objectType;
            internal set => objectType = value;
        }
        
        [SerializeField] private uint objectIdentifier;
        public uint ObjectIdentifier
        {
            get => objectIdentifier;
            internal set => objectIdentifier = value;
        }

        [SerializeField] private uint prefabIdentifier;
        public uint PrefabIdentifier => prefabIdentifier;

        public NetworkObject Parent { get; private set; }
        public uint? ParentIdentifier => Parent == null ? null : Parent.ObjectIdentifier;

        [SerializeField] private bool distributedAuthority;
        /// <summary>
        /// Whether the network object has distributed authority enabled
        /// </summary>
        public bool DistributedAuthority
        {
            get => distributedAuthority;
            set
            {
                if (value == distributedAuthority || IsSpawned) return;
                distributedAuthority = value;
            }
        }

        [SerializeField] private bool allowAuthorityRequests;
        /// <summary>
        /// Whether clients can manage authority or ownership over the network object using requests
        /// </summary>
        public bool AllowAuthorityRequests
        {
            get => allowAuthorityRequests;
            set
            {
                if (value == allowAuthorityRequests || IsSpawned) return;
                allowAuthorityRequests = value;
            }
        }

        private bool _isSpawned;
        /// <summary>
        /// Whether the network object is spawned locally
        /// </summary>
        public bool IsSpawned
        {
            get => _isSpawned;
            private set
            {
                if (value == _isSpawned) return;
                _isSpawned = value;
                
                ShouldReplicate = DistributedAuthority 
                    ? IsAuthor || AuthorID == 0 && networkManager.IsServer 
                    : networkManager.IsServer;

                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawned)
                {
                    OnNetworkSpawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnNetworkSpawned();

                    networkManager.Server.OnRemoteClientDisconnected += OnRemoteDisconnected;
                    networkManager.OnTickStarted += OnTickStarted;
                    networkManager.OnTickCompleted += OnTickCompleted;
                }
                else
                {
                    OnNetworkDespawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnNetworkDespawned();
                    
                    networkManager.Server.OnRemoteClientDisconnected -= OnRemoteDisconnected;
                    networkManager.OnTickStarted -= OnTickStarted;
                    networkManager.OnTickCompleted -= OnTickCompleted;
                    if (ObjectType == EObjectType.Instantiated)
                        Destroy(gameObject);
                }
            }
        }

        private bool _isSpawnedServer;
        internal bool IsSpawnedServer
        {
            get => _isSpawnedServer;
            set
            {
                if (value == _isSpawnedServer) return;
                _isSpawnedServer = value;
                
                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawnedServer)
                {
                    IsSpawned = true;
                    OnServerSpawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnServerSpawned();
                }
                else
                {
                    OnServerDespawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnServerDespawned();
                    IsSpawned = _isSpawnedClient;
                }
            }
        }

        private bool _isSpawnedClient;
        internal bool IsSpawnedClient
        {
            get => _isSpawnedClient;
            set
            {
                if (value == _isSpawnedClient) return;
                _isSpawnedClient = value;
                
                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawnedClient)
                {
                    IsSpawned = true;
                    OnClientSpawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnClientSpawned();
                }
                else
                {
                    OnClientDespawned?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnClientDespawned();
                    IsSpawned = _isSpawnedServer;
                }
            }
        }

        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public uint AuthorID { get; internal set; }
        /// <summary>
        /// The Id of the client with authority, 0 if no authority present
        /// </summary>
        public bool IsAuthor { get; internal set; }
        /// <summary>
        /// Whether someone has authority over the network object
        /// </summary>
        public bool IsAuthored => AuthorID != 0;
        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public uint OwnerID { get; internal set; }
        /// <summary>
        /// The Id of the client with ownership, 0 if no ownership present
        /// </summary>
        public bool IsOwner { get; internal set; }
        /// <summary>
        /// Whether someone has ownership over the network object
        /// </summary>
        public bool IsOwned => OwnerID != 0;
        /// <summary>
        /// Whether the local client has authority with distributed authority enabled, or the local server is started
        /// </summary>
        public bool ShouldReplicate { get; internal set; }
        
        /// <summary>
        /// Called on both client and server after the network object is spawned
        /// </summary>
        public event Action OnNetworkSpawned;
        /// <summary>
        /// Called on the server after the network object is spawned
        /// </summary>
        public event Action OnServerSpawned;
        /// <summary>
        /// Called on the client after the network object is spawned
        /// </summary>
        public event Action OnClientSpawned;
        /// <summary>
        /// Called on the server before the network object is despawned
        /// </summary>
        public event Action OnServerDespawned;
        /// <summary>
        /// Called on the client before the network object is despawned
        /// </summary>
        public event Action OnClientDespawned;
        /// <summary>
        /// Called on both client and server after network object is despawned
        /// </summary>
        public event Action OnNetworkDespawned;

        internal ushort OwnershipSequence;
        internal ushort AuthoritySequence;

        private UpdateObjectPacket.Builder _objectUpdates;
        
        #endregion
        
        #region lifecycle

#if UNITY_EDITOR
        [MenuItem("GameObject/ProteusNet/NetworkObject", false, 10)]
        public static void Initialize()
        {
            var go = new GameObject("NetworkObject");
            GameObjectUtility.EnsureUniqueNameForSibling(go);
            go.AddComponent<NetworkObject>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
        }

        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityUtilities.IsPrefabInEdit(this))
                return; // only update values in editor
            
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);
            objectIdentifier = Hashing.GetCRC32Hash(globalId.ToString());
            
            if (gameObject.scene.name == null)
            {
                objectType = EObjectType.Asset;
                prefabIdentifier = objectIdentifier;
                EditorUtility.SetDirty(this);
            }
            else
            {
                objectType = EObjectType.Placed;
                if (PrefabUtility.IsPartOfAnyPrefab(this))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<MonoNetworkManager>();
                if (networkManager == null)
                    throw new NullReferenceException("No NetworkManager available in the scene!");
            }
            
            Parent = transform.parent == null ? null : transform.parent.GetComponent<NetworkObject>();
            
            switch (objectType)
            {
                case EObjectType.Placed:
                    if (!networkManager.Objects.RegisterNetworkObject(this))
                        networkManager.Logger?.LogError($"An Id-collision has occurred for network objects with the Id {ObjectIdentifier}");
                    break;
                case EObjectType.Asset:
                case EObjectType.Instantiated:
                    objectType = EObjectType.Instantiated;
                    if (!networkManager.IsClient || networkManager.IsServer)
                        objectIdentifier = networkManager.Objects.GetNextNetworkObjectID();
                    if (!networkManager.Objects.RegisterNetworkObject(this))
                        networkManager.Logger?.LogError($"An Id-collision has occurred for network objects with the Id {ObjectIdentifier}");
                    break;
            }

            _objectUpdates = new(objectIdentifier);
        }

        private void OnEnable()
        {
            if (!IsSpawned || !ShouldReplicate)
                return;

            _objectUpdates.WithActiveUpdate(gameObject.activeInHierarchy);
        }

        private void OnDisable()
        {
            if (!IsSpawned || !ShouldReplicate)
                return;
            
            _objectUpdates.WithActiveUpdate(gameObject.activeInHierarchy);
        }

        private void OnTransformParentChanged()
        {
            if (!IsSpawned || !ShouldReplicate)
                return;
            
            Parent = transform.parent == null ? null : transform.parent.GetComponent<NetworkObject>();
            _objectUpdates.WithParentUpdate(ParentIdentifier);
        }

        private void OnDestroy()
        {
            if (!networkManager)
                return;
            
            // TODO : account for authority
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.DespawnNetworkObject(this);
            networkManager.Objects.ReleaseNetworkObject(objectIdentifier);
        }

        #endregion
        
        #region public methods
        
        public override int GetHashCode() => (int)ObjectIdentifier;
        public override bool Equals(object other) => Equals(other as NetworkObject);
        public bool Equals(NetworkObject other)
        {
            return other != null && gameObject == other.gameObject && ObjectIdentifier == other.ObjectIdentifier;
        }

        /// <summary>
        /// Spawns the network object on the local server and connected clients
        /// </summary>
        public void Spawn(uint authorID = 0)
        {
            if (networkManager.IsServer && !IsSpawned)
                networkManager.Server.SpawnNetworkObject(this, authorID);
        }

        /// <summary>
        /// Despawns the network object on the local server and connected clients
        /// </summary>
        public void Despawn()
        {
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.DespawnNetworkObject(this);
        }

        /// <summary>
        /// Gives authority over the network object to the given client
        /// </summary>
        /// <param name="clientID"></param>
        public void AssignAuthority(uint clientID)
        {
            if (!networkManager.IsServer)
            {
                Debug.LogError("The local server has to be started before authority over an object can be changed.");
                return;
            }
            
            if (!IsSpawned)
            {
                Debug.LogError("Authority can only be changed over a spawned object.");
                return;
            }

            if (!networkManager.Server.ConnectedClients.ContainsKey(clientID))
            {
                Debug.LogError("The client is not connected to the local server.");
                return;
            }

            if (AuthorID == clientID)
            {
                Debug.LogWarning("The client already has authority over the object.");
                return;
            }

            AuthoritySequence++;
            SetTakeAuthority(clientID, AuthoritySequence);
            var packet = new UpdateObjectPacket.Builder(ObjectIdentifier)
                .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
            networkManager.Server.UpdateNetworkObject(this, packet.Build());
        }

        /// <summary>
        /// Removes authority over the network object from any client
        /// </summary>
        public void RemoveAuthority()
        {
            if (!networkManager.IsServer)
            {
                Debug.LogError("The local server has to be started before authority over an object can be changed.");
                return;
            }
            
            if (!IsSpawned)
            {
                Debug.LogError("Authority can only be changed over a spawned object.");
                return;
            }
            
            if (AuthorID == 0)
            {
                Debug.LogWarning("No one has authority over the object.");
                return;
            }

            if (OwnerID == AuthorID)
            {
                OwnershipSequence++;
                SetReleaseOwnership(OwnershipSequence);
            }
            AuthoritySequence++;
            SetReleaseAuthority(AuthoritySequence);
            var packet = new UpdateObjectPacket.Builder(ObjectIdentifier)
                .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
            networkManager.Server.UpdateNetworkObject(this, packet.Build());
        }

        /// <summary>
        /// Gives ownership over the network object to the given client
        /// </summary>
        /// <param name="clientID"></param>
        public void AssignOwnership(uint clientID)
        {
            if (!networkManager.IsServer)
            {
                Debug.LogError("The local server has to be started before ownership over an object can be changed.");
                return;
            }
            
            if (!IsSpawned)
            {
                Debug.LogError("Ownership can only be changed over a spawned object.");
                return;
            }

            if (!networkManager.Server.ConnectedClients.ContainsKey(clientID))
            {
                Debug.LogError("The client is not connected to the local server.");
                return;
            }

            if (OwnerID == clientID)
            {
                Debug.LogWarning("The client already has authority over the object.");
                return;
            }

            OwnershipSequence++;
            SetTakeOwnership(clientID, OwnershipSequence);
            if (AuthorID != clientID)
            {
                AuthoritySequence++;
                SetTakeAuthority(clientID, AuthoritySequence);
            }
            var packet = new UpdateObjectPacket.Builder(ObjectIdentifier)
                .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
            networkManager.Server.UpdateNetworkObject(this, packet.Build());
        }

        /// <summary>
        /// Removes ownership over the network object from any client
        /// </summary>
        public void RemoveOwnership()
        {
            if (!networkManager.IsServer)
            {
                Debug.LogError("The local server has to be started before ownership over an object can be changed.");
                return;
            }
            
            if (!IsSpawned)
            {
                Debug.LogError("Ownership can only be changed over a spawned object.");
                return;
            }
            
            if (OwnerID == 0)
            {
                Debug.LogWarning("No one has ownership over the object.");
                return;
            }
            
            OwnershipSequence++;
            SetReleaseOwnership(OwnershipSequence);
            var packet = new UpdateObjectPacket.Builder(ObjectIdentifier)
                .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
            networkManager.Server.UpdateNetworkObject(this, packet.Build());
        }
        
        /// <summary>
        /// Requests authority over the network object for the local client
        /// </summary>
		public void RequestAuthority()
		{
            if (!networkManager.IsClient)
            {
                Debug.LogError("The local client has to be authenticated before authority can be requested over an object.");
                return;
            }

            if (!AllowAuthorityRequests || !IsSpawned)
            {
                Debug.LogError("Authority can only be requested over a spawned object.");
                return;
            }

            if (IsAuthor || OwnerID != 0)
            {
                Debug.LogError("The object already has an owner or you already have authority.");
                return;
            }

            var newAuthority = (ushort)(AuthoritySequence + 1);
            networkManager.Client.UpdateDistributedAuthority(this, 
                DistributedAuthorityPacket.EType.RequestAuthority, newAuthority, OwnershipSequence);
        }

        /// <summary>
        /// Releases authority over the network object by the local client
        /// </summary>
		public void ReleaseAuthority()
		{
            if (!IsAuthor || IsOwner)
            {
                Debug.LogError("You have to have authority over the object before you can relinquish it.");
                return;
            }

            var newAuthority = (ushort)(AuthoritySequence + 1);
            networkManager.Client.UpdateDistributedAuthority(this, 
                DistributedAuthorityPacket.EType.ReleaseAuthority, newAuthority, OwnershipSequence);
        }
        
        /// <summary>
        /// Requests ownership over the network object for the local client
        /// </summary>
        public void RequestOwnership()
        {
            if (!networkManager.IsClient)
            {
                Debug.LogError("The local client has to be authenticated before ownership can be requested over an object.");
                return;
            }

            if (!AllowAuthorityRequests || !IsSpawned)
            {
                Debug.LogError("Ownership can only be requested over a spawned object.");
                return;
            }

            if (OwnerID > 0)
            {
                Debug.LogError("The object already has an owner.");
                return;
            }

            var newAuthority = !IsAuthor ? (ushort)(AuthoritySequence + 1) : AuthoritySequence;
            var newOwnership = (ushort)(OwnershipSequence + 1);
            networkManager.Client.UpdateDistributedAuthority(this, 
                DistributedAuthorityPacket.EType.RequestOwnership, newAuthority, newOwnership);
        }

        /// <summary>
        /// Releases ownership over the network object by the local client
        /// </summary>
        public void ReleaseOwnership()
        {
            if (!IsOwner)
            {
                Debug.LogError("You have to have ownership over the object before you can relinquish it.");
                return;
            }

            var newOwnership = (ushort)(OwnershipSequence + 1);
            networkManager.Client.UpdateDistributedAuthority(this, 
                DistributedAuthorityPacket.EType.ReleaseOwnership, AuthoritySequence, newOwnership);
        }
        
        #endregion
        
        #region internal methods

        private void OnTickStarted(uint tick)
        {
            if (ShouldReplicate)
            {
                var updateBuild = _objectUpdates.Build();
                if (updateBuild.Flags != UpdateObjectPacket.EFlags.Nothing)
                {
                    if (networkManager.IsServer)
                        networkManager.Server.UpdateNetworkObject(this, updateBuild);
                    else
                        networkManager.Client.UpdateNetworkObject(this, updateBuild);
                    _objectUpdates.Reset();
                }
            }
            
            var behaviours = GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
                behaviour.OnTickStarted(tick);
        }

        private void OnTickCompleted(uint tick)
        {
            var behaviours = GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
                behaviour.OnTickCompleted(tick);
        }
        
        private void OnRemoteDisconnected(uint clientID)
        {
            if (AuthorID != clientID && OwnerID != clientID)
                return;
            
            if (AuthorID == clientID)
            {
                AuthoritySequence++;
                SetReleaseAuthority(AuthoritySequence);
            }
            if (OwnerID == clientID)
            {
                OwnershipSequence++;
                SetReleaseOwnership(OwnershipSequence);
            }
            
            var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
            networkManager.Server.UpdateNetworkObject(this, update.Build());
        }

        internal void OnRemoteSpawn(uint clientID)
        {
            var behaviours = GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
                behaviour.OnRemoteSpawn(clientID);
        }

        internal void OnRemoteDespawn(uint clientID)
        {
            var behaviours = GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
                behaviour.OnRemoteDespawn(clientID);
        }

        internal void UpdateDistributedAuthorityServer(uint clientID, DistributedAuthorityPacket packet)
        {
			switch (packet.Type)
			{
				case DistributedAuthorityPacket.EType.RequestAuthority:
					if (OwnerID != 0 || AuthorID == clientID || !IsNextNumber(packet.AuthoritySequence, AuthoritySequence) || !AllowAuthorityRequest(clientID))
					{   // inform requesting client of current sequence, since request is invalid
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(clientID, this, update.Build());
					}
					else
					{   // update authority and inform all clients
						SetTakeAuthority(clientID, packet.AuthoritySequence);
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(this, update.Build());
					}
					break;
				case DistributedAuthorityPacket.EType.ReleaseAuthority:
					if (AuthorID != clientID || !IsNextNumber(packet.AuthoritySequence, AuthoritySequence))
					{   // inform requesting client of current sequence, since request is invalid
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(clientID, this, update.Build());
					}
					else
					{    // update authority and inform all clients
						SetReleaseAuthority(packet.AuthoritySequence);
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(this, update.Build());
					}
					break;
                case DistributedAuthorityPacket.EType.RequestOwnership:
                    if (OwnerID != 0 || !IsNextNumber(packet.OwnershipSequence, OwnershipSequence) || !AllowOwnershipRequest(clientID))
                    {   // inform requesting client of current sequence, since request is invalid
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(clientID, this, update.Build());
                    }
                    else
                    {    // update authority and inform all clients
                        SetTakeOwnership(clientID, packet.OwnershipSequence);
                        SetTakeAuthority(clientID, packet.AuthoritySequence);
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(this, update.Build());
                    }
                    break;
                case DistributedAuthorityPacket.EType.ReleaseOwnership:
                    if (OwnerID != clientID || !IsNextNumber(packet.OwnershipSequence, OwnershipSequence))
                    {   // inform requesting client of current sequence, since request is invalid
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(clientID, this, update.Build());
                    }
                    else
                    {    // update authority and inform all clients
                        SetReleaseOwnership(packet.OwnershipSequence);
                        var update = new UpdateObjectPacket.Builder(ObjectIdentifier)
                            .WithAuthorityUpdate(AuthorID, AuthoritySequence, OwnerID, OwnershipSequence);
                        networkManager.Server.UpdateNetworkObject(this, update.Build());
                    }
                    break;
			}
        }

        internal void UpdateDistributedAuthorityClient(uint authorID, ushort authoritySequence, uint ownerID, ushort ownershipSequence)
        {
            var prevAuthor = AuthorID;
            AuthorID = authorID;
            AuthoritySequence = authoritySequence;
            IsAuthor = networkManager.IsClient && 
                       networkManager.Client.ClientID == authorID;

            var prevOwner = OwnerID;
            OwnerID = ownerID;
            OwnershipSequence = ownershipSequence;
            IsOwner = networkManager.IsClient && 
                      networkManager.Client.ClientID == ownerID;
            
            ShouldReplicate = DistributedAuthority 
                ? IsAuthor || AuthorID == 0 && networkManager.IsServer 
                : networkManager.IsServer;

            StatusChanged(prevAuthor, prevOwner);
        }
        
        #endregion
        
        #region private methods

        private bool AllowAuthorityRequest(uint clientID)
        {
            if (!AllowAuthorityRequests) return false;
            
            var behaviours = GetComponents<NetworkBehaviour>();
            return behaviours.All(behaviour => behaviour.OnAuthorityRequested(clientID));
        }
        
        private bool AllowOwnershipRequest(uint clientID)
        {
            if (!AllowAuthorityRequests) return false;
            
            var behaviours = GetComponents<NetworkBehaviour>();
            return behaviours.All(behaviour => behaviour.OnOwnershipRequested(clientID));
        }
        
        private void SetTakeAuthority(uint clientID, ushort authoritySequence)
        {
            var prevAuthor = AuthorID;
            AuthorID = clientID;
            AuthoritySequence = authoritySequence;
            IsAuthor = networkManager.IsClient && 
                       networkManager.Client.ClientID == clientID;
            
            ShouldReplicate = DistributedAuthority 
                ? IsAuthor || AuthorID == 0 && networkManager.IsServer 
                : networkManager.IsServer;
            
            StatusChanged(prevAuthor, OwnerID);
        }

        private void SetReleaseAuthority(ushort authoritySequence)
        {
            var prevAuthor = AuthorID;
            AuthorID = 0;
            AuthoritySequence = authoritySequence;
            IsAuthor = false;
            
            ShouldReplicate = DistributedAuthority 
                ? IsAuthor || AuthorID == 0 && networkManager.IsServer 
                : networkManager.IsServer;
            
            StatusChanged(prevAuthor, OwnerID);
        }
        
        private void SetTakeOwnership(uint clientID, ushort ownershipSequence)
        {
            var prevOwner = OwnerID;
            OwnerID = clientID;
            OwnershipSequence = ownershipSequence;
            IsOwner = networkManager.IsClient && 
                      networkManager.Client.ClientID == clientID;
            
            StatusChanged(AuthorID, prevOwner);
        }

        private void SetReleaseOwnership(ushort ownershipSequence)
        {
            var prevOwner = OwnerID;
            OwnerID = 0;
            OwnershipSequence = ownershipSequence;
            IsOwner = false;
            
            StatusChanged(AuthorID, prevOwner);
        }

        private void StatusChanged(uint authorID, uint ownerID)
        {
            var behaviours = GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (authorID != AuthorID)
                    behaviour.OnAuthorityChanged(authorID);
                if (ownerID != OwnerID)
                    behaviour.OnOwnershipChanged(ownerID);
            }
        }

        private static bool IsNextNumber(ushort a, ushort b)
        {
            return (ushort)((b + 1) % (ushort.MaxValue + 1)) == a;
        }
        
        #endregion
    }
}
