using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        #region fields and properties
        
        private NetworkObject _networkObject;
        public NetworkObject NetworkObject
        {
            get
            {
                if (_networkObject != null) return _networkObject;
                return _networkObject = GetComponent<NetworkObject>();
            }
        }
        
        public MonoNetworkManager NetworkManager => NetworkObject.NetworkManager;
        
        /// <summary>
        /// Whether a local server is started
        /// </summary>
        public bool IsServer => NetworkManager.IsServer;
        /// <summary>
        /// Whether a local client is started and authenticated
        /// </summary>
        public bool IsClient => NetworkManager.IsClient;
        /// <summary>
        /// Whether a local server is started or local client is authenticated
        /// </summary>
        public bool IsOnline => NetworkManager.IsOnline;
        /// <summary>
        /// Whether a local server is started and local client is authenticated
        /// </summary>
        public bool IsHost => NetworkManager.IsHost;

        /// <summary>
        /// Whether the network object has distributed authority enabled
        /// </summary>
        public bool DistributedAuthority => NetworkObject.DistributedAuthority;
        /// <summary>
        /// Whether the network object is spawned locally
        /// </summary>
        public bool IsSpawned => NetworkObject.IsSpawned;

        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public bool IsAuthor => NetworkObject.IsAuthor;
        /// <summary>
        /// The Id of the client with authority, 0 if no authority present
        /// </summary>
        public uint AuthorID => NetworkObject.AuthorID;
        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public bool IsOwner => NetworkObject.IsOwner;
        /// <summary>
        /// The Id of the client with ownership, 0 if no ownership present 
        /// </summary>
        public uint OwnerID => NetworkObject.OwnerID;
        /// <summary>
        /// Whether the local client has authority, or no one has authority and the local server is started
        /// </summary>
        public bool HasAuthority => NetworkObject.HasAuthority;
        
        #endregion
        
        #region lifecycle

        /// <summary>
        /// Called on both client and server after the network object is spawned
        /// </summary>
        /// <remarks>This is called before other callbacks</remarks>
        public virtual void OnNetworkSpawned() {}
        /// <summary>
        /// Called on the server after the network object is spawned
        /// </summary>
        public virtual void OnServerSpawned() {}
        /// <summary>
        /// Called on the client after the network object is spawned
        /// </summary>
        public virtual void OnClientSpawned() {}
        /// <summary>
        /// Called on the server before the network object is despawned
        /// </summary>
        public virtual void OnServerDespawned() {}
        /// <summary>
        /// Called on the client before the network object is despawned
        /// </summary>
        public virtual void OnClientDespawned() {}
        /// <summary>
        /// Called on both client and server before network object is despawned
        /// </summary>
        /// <remarks>This is called after other callbacks</remarks>
        public virtual void OnNetworkDespawned() {}
        /// <summary>
        /// Called on the server after a spawn message for the network object was sent to a remote client
        /// </summary>
        /// <param name="clientID"></param>
        public virtual void OnRemoteSpawn(uint clientID) {}
        /// <summary>
        /// Called on the server after a client requests authority over the network object
        /// </summary>
        /// <param name="clientID"></param>
        /// <returns>If the request should be allowed</returns>
        public virtual bool OnAuthorityRequested(uint clientID) => true;
        /// <summary>
        /// Called once the authority over the network object changed
        /// </summary>
        /// <param name="prevClientID"></param>
        public virtual void OnAuthorityChanged(uint prevClientID) {}
        /// <summary>
        /// Called on the server after a client requests ownership over the network object
        /// </summary>
        /// <param name="clientID"></param>
        /// <returns>If the request should be allowed</returns>
        public virtual bool OnOwnershipRequested(uint clientID) => true;
        /// <summary>
        /// Called once the ownership over the network object changed
        /// </summary>
        /// <param name="prevClientID"></param>
        public virtual void OnOwnershipChanged(uint prevClientID) {}
        /// <summary>
        /// Called at the start of the tick, where packets can still be added for sending
        /// </summary>
        /// <param name="tick"></param>
        public virtual void OnTickStarted(uint tick) {}
        /// <summary>
        /// Called at the end of the tick, when all packets have been handled
        /// </summary>
        /// <param name="tick"></param>
        public virtual void OnTickCompleted(uint tick) {}

        #endregion
    }
}
