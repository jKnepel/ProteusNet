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
        /// </summary>
        Placed,
        /// <summary>
        /// NetworkObjects which are instantiated during the playmode.
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

        public bool IsSpawned { get; private set; }

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

        public event Action OnNetworkStarted;
        public event Action OnServerStarted;
        public event Action OnClientStarted;
        public event Action OnServerStopped;
        public event Action OnClientStopped;
        public event Action OnNetworkStopped;
        
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
                objectType = EObjectType.Placed;
                if (PrefabUtility.IsPartOfAnyPrefab(this))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
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
            
            Parent = transform.parent == null ? null : transform.parent.GetComponent<NetworkObject>();

            switch (objectType)
            {
                case EObjectType.Placed:
                    networkManager.Objects.RegisterNetworkObject(this);
                    break;
                case EObjectType.Asset:
                case EObjectType.Instantiated:
                    objectType = EObjectType.Instantiated;
                    if (!networkManager.IsClient || networkManager.IsServer)
                        objectIdentifier = networkManager.Objects.GetNextNetworkObjectID();
                    networkManager.Objects.RegisterNetworkObject(this);
                    break;
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            if (!networkManager)
                return;

            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            if (!networkManager)
                return;
            
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        private void OnTransformParentChanged()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            Parent = transform.parent == null ? null : transform.parent.GetComponent<NetworkObject>();
            
            if (!networkManager)
                return;
            
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.UpdateNetworkObject(this);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            if (!networkManager)
                return;
            
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.DespawnNetworkObject(this);
            networkManager.Objects.ReleaseNetworkObjectID(objectIdentifier);
        }

        #endregion
        
        #region public methods
        
        public override int GetHashCode() => (int)ObjectIdentifier;
        public override bool Equals(object other) => Equals(other as NetworkObject);
        public bool Equals(NetworkObject other)
        {
            return other != null && gameObject == other.gameObject && ObjectIdentifier == other.ObjectIdentifier;
        }

        public void Spawn()
        {
            if (networkManager.IsServer && !IsSpawned)
                networkManager.Server.SpawnNetworkObject(this);
        }

        public void Despawn()
        {
            if (networkManager.IsServer && IsSpawned)
                networkManager.Server.DespawnNetworkObject(this);
        }
        
        #endregion
        
        #region internal
        
        internal void InternalSpawnServer()
        {
            IsSpawned = true;
            
            OnNetworkStarted?.Invoke();
            OnServerStarted?.Invoke();
        }
        
        internal void InternalSpawnClient()
        {
            IsSpawned = true;
            
            if (!networkManager.IsServer)
                OnNetworkStarted?.Invoke();
            OnClientStarted?.Invoke();
        }

        internal void InternalDespawnServer()
        {
            IsSpawned = false;
            
            if (!networkManager.IsClient)
            {
                switch (ObjectType)
                {
                    case EObjectType.Placed:
                        // TODO : handle / reset placed objects ?
                        break;
                    case EObjectType.Instantiated:
                        Destroy(gameObject);
                        break;
                    default: return;
                }
            }
            
            OnServerStopped?.Invoke();
            if (!networkManager.IsClient)
                OnNetworkStopped?.Invoke();
        }
        
        internal void InternalDespawnClient()
        {
            IsSpawned = false;
            
            switch (ObjectType)
            {
                case EObjectType.Placed:
                    // TODO : handle / reset placed objects ?
                    break;
                case EObjectType.Instantiated:
                    Destroy(gameObject);
                    break;
                default: return;
            }

            OnClientStopped?.Invoke();
            OnNetworkStopped?.Invoke();
        }
        
        #endregion
    }
}
