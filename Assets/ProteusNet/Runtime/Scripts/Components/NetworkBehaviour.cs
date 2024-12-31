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
        
        #endregion
        
        #region lifecycle

        public virtual void OnNetworkStarted() {}
        public virtual void OnServerStarted() {}
        public virtual void OnClientStarted() {}
        public virtual void OnServerStopped() {}
        public virtual void OnClientStopped() {}
        public virtual void OnNetworkStopped() {}
        public virtual void OnRemoteSpawn(uint clientID) {}
        
        #endregion
    }
}
