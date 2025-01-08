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
        
        public bool IsServer => NetworkManager.IsServer;
        public bool IsClient => NetworkManager.IsClient;
        public bool IsOnline => NetworkManager.IsOnline;
        public bool IsHost => NetworkManager.IsHost;

        public bool DistributedAuthority => NetworkObject.DistributedAuthority;
        public bool IsSpawned => NetworkObject.IsSpawned;

        public bool IsAuthor => NetworkObject.IsAuthor;
        public uint AuthorID => NetworkObject.AuthorID;
        public bool IsOwner => NetworkObject.IsOwner;
        public uint OwnerID => NetworkObject.OwnerID;
        public bool HasAuthority => NetworkObject.HasAuthority;
        
        #endregion
        
        #region lifecycle

        public virtual void OnNetworkStarted() {}
        public virtual void OnServerStarted() {}
        public virtual void OnClientStarted() {}
        public virtual void OnServerStopped() {}
        public virtual void OnClientStopped() {}
        public virtual void OnNetworkStopped() {}
        public virtual void OnRemoteSpawn(uint clientID) {}
        public virtual bool OnAuthorityRequested(uint clientID) => true;
        public virtual bool OnOwnershipRequested(uint clientID) => true;

        #endregion
    }
}
