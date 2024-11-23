using System;
using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
using UnityEngine;
#if UNITY_EDITOR
using jKnepel.ProteusNet.Utilities;
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components
{
    public enum ENetworkObjectType
    {
        /// <summary>
        /// NetworkObjects that exist as serialized asset outside of a scene.
        /// </summary>
        Asset,
        /// <summary>
        /// NetworkObjects which are placed in-scene during the editing of a scene.
        /// Its ID will always start with a 0-bit.
        /// </summary>
        Placed,
        /// <summary>
        /// NetworkObjects which are instantiated during the playmode.
        /// Its ID will always start with a 1-bit.
        /// </summary>
        Instantiated
    }
    
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("ProteusNet/Network Object")]
    public class NetworkObject : MonoBehaviour
    {
        #region attributes
        
        [SerializeField] private MonoNetworkManager networkManager;
        [SerializeField] private ENetworkObjectType objectType;
        [SerializeField] private uint prefabIdentifier;
        [SerializeField] private uint objectIdentifier;

        private NetworkObject _parent;
        private bool _enabled;

        public uint Identifier => objectIdentifier;

        public uint? ParentIdentifier { get; private set; }
        private NetworkObject Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                if (value == null) return;
                transform.parent = value.transform;
                ParentIdentifier = value.Identifier;
            }
        }

        public event Action OnNetworkStarted;
        public event Action OnServerStarted;
        public event Action OnClientStarted;
        
        #endregion
        
        #region lifecycle

#if UNITY_EDITOR
        [MenuItem("GameObject/ProteusNet/NetworkObject", false, 10)]
        public static void CreateNetworkObject()
        {
            var go = new GameObject("NetworkObject");
            GameObjectUtility.EnsureUniqueNameForSibling(go);
            go.AddComponent<NetworkObject>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create new NetworkObject");
        }
        
        private void OnValidate()
        {
            // only update id during editor
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            // only update in scene objects
            if (gameObject.scene.name == null || UnityUtilities.IsPrefabInEdit(this)) 
                return;
            
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);
            objectIdentifier = Hashing.GetCRC32Hash(globalId.ToString());
            objectIdentifier &= ~(1u << 31);
            objectType = ENetworkObjectType.Placed;
            
            if (PrefabUtility.IsPartOfAnyPrefab(this))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
            if (gameObject.scene.name == null || UnityUtilities.IsPrefabInEdit(this))
                return;
#endif

            if (networkManager == null)
            {
                networkManager = FindObjectOfType<MonoNetworkManager>();
                if (networkManager == null)
                    throw new NullReferenceException("No NetworkManager available in the scene!");
            }
            
            switch (objectType)
            {
                case ENetworkObjectType.Placed:
                    networkManager.Objects.RegisterNetworkObjectID(objectIdentifier, this);
                    if (networkManager.IsServer)
                    {
                        networkManager.Server.SpawnNetworkObject(this);
                    }
                    else
                    {
                        networkManager.Server.OnLocalStateUpdated += LocalServerStarted;
                        gameObject.SetActive(false);
                    }
                    break;
                case ENetworkObjectType.Instantiated:
                    objectIdentifier = networkManager.Objects.GetNextNetworkObjectID(this);
                    break;
            }

            return;
            void LocalServerStarted(ELocalServerConnectionState state)
            {
                if (state != ELocalServerConnectionState.Started) return;
                networkManager.Server.OnLocalStateUpdated -= LocalServerStarted;
                networkManager.Server.SpawnNetworkObject(this);
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!networkManager.IsServer)
                return;

            _enabled = true;
            // TODO : notify of update
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!networkManager.IsServer)
                return;
            
            _enabled = false;
            // TODO : notify of update
        }

        private void OnTransformParentChanged()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!networkManager.IsServer)
                return;
                
            if (transform.parent != null)
                Parent = transform.parent.GetComponent<NetworkObject>();
            // TODO : notify of update
        }

        private void OnDestroy()
        {
        }

        #endregion
        
        #region internal

        protected internal void SpawnOnServer()
        {
            _enabled = true;
            if (transform.parent != null)
                Parent = transform.parent.GetComponent<NetworkObject>();
            gameObject.SetActive(true);
            
            OnNetworkStarted?.Invoke();
            OnServerStarted?.Invoke();
            if (networkManager.IsClient)
            {
                // TODO : also handle cases where local client is started later
                OnClientStarted?.Invoke();
            }
        }
        
        protected internal void SpawnOnClient(NetworkObject parent = null)
        {
            _enabled = true;
            Parent = parent;
            gameObject.SetActive(true);
            
            OnNetworkStarted?.Invoke();
            OnClientStarted?.Invoke();
        }
        
        #endregion
    }
}
