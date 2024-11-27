using jKnepel.ProteusNet.Managing;
using System;
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
    public class NetworkObject : MonoBehaviour, IEquatable<NetworkObject>
    {
        #region attributes
        
        [SerializeField] private MonoNetworkManager networkManager;
        [SerializeField] private EObjectType objectType;
        [SerializeField] private uint objectIdentifier;
        [SerializeField] private uint prefabIdentifier;
        private uint? _parentIdentifier;
        private bool _isSpawned;

        public bool IsSpawned => _isSpawned;
        public EObjectType ObjectType => objectType;
        public uint ObjectIdentifier => objectIdentifier;
        public uint PrefabIdentifier => prefabIdentifier;
        public uint? ParentIdentifier => _parentIdentifier;
        
        private NetworkObject _parent;
        public NetworkObject Parent
        {
            get => _parent;
            private set
            {
                if (value == _parent)
                    return;
                
                _parent = value;
                _parentIdentifier = value == null ? null : value.ObjectIdentifier;
            }
        }

        public event Action OnNetworkStarted;
        public event Action OnServerStarted;
        public event Action OnClientStarted;
        
        #endregion
        
        #region lifecycle

        public override int GetHashCode() => (int)ObjectIdentifier;
        public override bool Equals(object other) => Equals(other as NetworkObject);
        public bool Equals(NetworkObject other)
        {
            return other != null && gameObject == other.gameObject && ObjectIdentifier == other.ObjectIdentifier;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/ProteusNet/NetworkObject", false, 10)]
        public static void Initialize()
        {
            var go = new GameObject("NetworkObject");
            GameObjectUtility.EnsureUniqueNameForSibling(go);
            go.AddComponent<NetworkObject>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create new NetworkObject");
        }

        protected virtual void OnValidate()
        {
            // only update values during editor
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityUtilities.IsPrefabInEdit(this))
                return;
            
            if (gameObject.scene.name == null)
            {
                objectType = EObjectType.Asset;
                EditorUtility.SetDirty(this);
            }
            else
            {
                var globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);
                objectIdentifier = Hashing.GetCRC32Hash(globalId.ToString());
                objectIdentifier &= ~(1u << 31);
                objectType = EObjectType.Placed;
                if (PrefabUtility.IsPartOfAnyPrefab(this))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif

        protected virtual void Awake()
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
            
            gameObject.SetActive(false);
            Parent = transform.parent == null ? null 
                : transform.parent.GetComponent<NetworkObject>();
            
            if (objectType == EObjectType.Placed)
            {   // handle in scene placed network objects
                networkManager.Objects.RegisterNetworkObjectID(objectIdentifier, this);
                if (networkManager.IsServer)
                    networkManager.Server.SpawnPlacedNetworkObject(this);
                else
                    networkManager.Server.OnLocalServerStarted += LocalServerStarted;
                return;
            }

            return;
            void LocalServerStarted()
            {
                networkManager.Server.OnLocalServerStarted -= LocalServerStarted;
                networkManager.Server.SpawnPlacedNetworkObject(this);
            }
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnTransformParentChanged()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            Parent = transform.parent == null ? null 
                : transform.parent.GetComponent<NetworkObject>();

            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnDestroy()
        {
        }

        #endregion
        
        #region internal
        
        // TODO : make logic self-managed instead of calling manually...?

        internal void SpawnOnServer()
        {
            _isSpawned = true;
            
            OnNetworkStarted?.Invoke();
            OnServerStarted?.Invoke();
        }
        
        internal void SpawnOnClient()
        {
            _isSpawned = true;
            
            if (!networkManager.IsServer)
                OnNetworkStarted?.Invoke();
            OnClientStarted?.Invoke();
        }

        internal void InitializeInstantiatedServer()
        {
            objectType = EObjectType.Instantiated;
            objectIdentifier = networkManager.Objects.GetNextNetworkObjectID(this);
        }

        internal void InitializeInstantiatedClient(uint identifier)
        {
            objectType = EObjectType.Instantiated;
            objectIdentifier = identifier;
            networkManager.Objects.RegisterNetworkObjectID(identifier, this);
        }
        
        #endregion
    }
}
