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
        /// The Id of the local client
        /// </summary>
        public uint LocalClientID => NetworkManager.Client.ClientID;

        /// <summary>
        /// Whether the network object has distributed authority enabled
        /// </summary>
        public bool DistributedAuthority => NetworkObject.DistributedAuthority;
        /// <summary>
        /// Whether the network object is spawned locally
        /// </summary>
        public bool IsSpawned => NetworkObject.IsSpawned;

        /// <summary>
        /// The Id of the client with authority, 0 if no authority present
        /// </summary>
        public uint AuthorID => NetworkObject.AuthorID;
        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public bool IsAuthor => NetworkObject.IsAuthor;
        /// <summary>
        /// Whether someone has authority over the network object
        /// </summary>
        public bool IsAuthored => NetworkObject.IsAuthored;
        /// <summary>
        /// The Id of the client with ownership, 0 if no ownership present 
        /// </summary>
        public uint OwnerID => NetworkObject.OwnerID;
        /// <summary>
        /// Whether the local client has authority over the network object
        /// </summary>
        public bool IsOwner => NetworkObject.IsOwner;
        /// <summary>
        /// Whether someone has ownership over the network object
        /// </summary>
        public bool IsOwned => NetworkObject.IsOwned;
        /// <summary>
        /// Whether the local client has authority with distributed authority enabled, or the local server is started
        /// </summary>
        public bool ShouldReplicate => NetworkObject.ShouldReplicate;
        
        #endregion
        
        #region lifecycle
        
        // ------- Networking --------

        /// <summary>
        /// Spawns the network object on the local server and connected clients
        /// </summary>
        public void Spawn(uint authorID = 0) => NetworkObject.Spawn(authorID);
        /// <summary>
        /// Despawns the network object on the local server and connected clients
        /// </summary>
        public void Despawn() => NetworkObject.Despawn();

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
        /// Called on both client and server after network object is despawned
        /// </summary>
        /// <remarks>This is called after other callbacks</remarks>
        public virtual void OnNetworkDespawned() {}
        /// <summary>
        /// Called on the server after a spawn message for the network object was sent to a remote client
        /// </summary>
        /// <param name="clientID"></param>
        public virtual void OnRemoteSpawn(uint clientID) {}
        /// <summary>
        /// Called on the server before a despawn message for the network object is send to a remote client
        /// </summary>
        /// <param name="clientID"></param>
        public virtual void OnRemoteDespawn(uint clientID) {}
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
        
        // ------- Authority --------

        /// <summary>
        /// Gives authority over the network object to the given client
        /// </summary>
        /// <param name="clientID"></param>
        public void AssignAuthority(uint clientID) => NetworkObject.AssignAuthority(clientID);
        /// <summary>
        /// Removes authority over the network object from any client
        /// </summary>
        public void RemoveAuthority() => NetworkObject.RemoveAuthority();
        /// <summary>
        /// Gives ownership over the network object to the given client
        /// </summary>
        /// <param name="clientID"></param>
        public void AssignOwnership(uint clientID) => NetworkObject.AssignOwnership(clientID);
        /// <summary>
        /// Removes ownership over the network object from any client
        /// </summary>
        public void RemoveOwnership() => NetworkObject.RemoveOwnership();

        /// <summary>
        /// Requests authority over the network object for the local client
        /// </summary>
        public void RequestAuthority() => NetworkObject.RequestAuthority();
        /// <summary>
        /// Releases authority over the network object by the local client
        /// </summary>
        public void ReleaseAuthority() => NetworkObject.ReleaseAuthority();
        /// <summary>
        /// Requests ownership over the network object for the local client
        /// </summary>
        public void RequestOwnership() => NetworkObject.RequestOwnership();
        /// <summary>
        /// Releases ownership over the network object by the local client
        /// </summary>
        public void ReleaseOwnership() => NetworkObject.ReleaseOwnership();
        
        /// <summary>
        /// Called on the server after a client requests authority over the network object
        /// </summary>
        /// <param name="clientID"></param>
        /// <returns>If the request should be allowed</returns>
        public virtual bool OnAuthorityRequested(uint clientID) => NetworkObject.AllowAuthorityRequests;
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
        public virtual bool OnOwnershipRequested(uint clientID) => NetworkObject.AllowAuthorityRequests;
        /// <summary>
        /// Called once the ownership over the network object changed
        /// </summary>
        /// <param name="prevClientID"></param>
        public virtual void OnOwnershipChanged(uint prevClientID) {}

        #endregion
    }
}
