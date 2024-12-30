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

        private bool _isSpawned;
        public bool IsSpawned
        {
            get => _isSpawned;
            private set
            {
                if (value == _isSpawned) return;
                _isSpawned = value;

                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawned)
                {
                    OnNetworkStarted?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnNetworkStarted();
                }
                else
                {
                    OnNetworkStopped?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnNetworkStopped();
                    
                    if (ObjectType == EObjectType.Instantiated)
                        Destroy(gameObject);
                }
            }
        }

        private bool _isSpawnedServer;
        public bool IsSpawnedServer
        {
            get => _isSpawnedServer;
            internal set
            {
                if (value == _isSpawnedServer) return;
                _isSpawnedServer = value;
                
                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawnedServer)
                {
                    IsSpawned = true;
                    OnServerStarted?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnServerStarted();
                }
                else
                {
                    OnServerStopped?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnServerStopped();
                    IsSpawned = _isSpawnedClient;
                }
            }
        }

        private bool _isSpawnedClient;
        public bool IsSpawnedClient
        {
            get => _isSpawnedClient;
            internal set
            {
                if (value == _isSpawnedClient) return;
                _isSpawnedClient = value;
                
                var behaviours = GetComponents<NetworkBehaviour>();
                if (_isSpawnedClient)
                {
                    IsSpawned = true;
                    OnClientStarted?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnClientStarted();
                }
                else
                {
                    OnClientStopped?.Invoke();
                    foreach (var behaviour in behaviours)
                        behaviour.OnClientStopped();
                    IsSpawned = _isSpawnedServer;
                }
            }
        }
        
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
    }
}
