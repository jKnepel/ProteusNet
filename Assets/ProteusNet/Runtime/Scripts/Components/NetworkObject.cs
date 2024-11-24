using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
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
        public uint Identifier => objectIdentifier;
        public uint PrefabIdentifier => prefabIdentifier;
        public uint? ParentIdentifier => _parentIdentifier;
        
        private NetworkObject _parent;
        public NetworkObject Parent
        {
            get => _parent;
            internal set
            {
                _parent = value;
                if (value == null)
                {
                    transform.parent = null;
                    _parentIdentifier = null;
                }
                else
                {
                    transform.parent = value.transform;
                    _parentIdentifier = value.Identifier;
                }
            }
        }

        public event Action OnNetworkStarted;
        public event Action OnServerStarted;
        public event Action OnClientStarted;
        
        #endregion
        
        #region lifecycle

        public override int GetHashCode() => (int)Identifier;
        public override bool Equals(object other) => Equals(other as NetworkObject);
        public bool Equals(NetworkObject other)
        {
            return other != null && gameObject == other.gameObject && Identifier == other.Identifier;
        }

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

            if (!networkManager.IsServer || !IsSpawned)
                return;
            
            networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!networkManager.IsServer || !IsSpawned)
                return;
            
            networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnTransformParentChanged()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!networkManager.IsServer || !IsSpawned)
                return;
            
            Parent = transform.parent == null ? null 
                : transform.parent.GetComponent<NetworkObject>();
            networkManager.Server.UpdateNetworkObject(this);
        }

        protected virtual void OnDestroy()
        {
        }

        #endregion
        
        #region internal
        
        // TODO : make logic self-managed instead of calling manually...?

        protected internal void SpawnOnServer()
        {
            Parent = transform.parent == null ? null 
                : transform.parent.GetComponent<NetworkObject>();
            gameObject.SetActive(true);
            _isSpawned = true;
            
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
            Parent = parent;
            gameObject.SetActive(true);
            _isSpawned = true;
            
            OnNetworkStarted?.Invoke();
            OnClientStarted?.Invoke();
        }

        protected internal void InitializeInstantiated()
        {
            objectType = EObjectType.Instantiated;
            objectIdentifier = networkManager.Objects.GetNextNetworkObjectID(this);
        }

        protected internal void InitializeInstantiated(uint objectIdentifier)
        {
            objectType = EObjectType.Instantiated;
            this.objectIdentifier = objectIdentifier;
        }
        
        #endregion
    }
}
