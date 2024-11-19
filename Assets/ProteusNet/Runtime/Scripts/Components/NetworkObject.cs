using jKnepel.ProteusNet.Managing;
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
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private MonoNetworkManager networkManager;
        [SerializeField] private ENetworkObjectType objectType = ENetworkObjectType.Instantiated;
        [SerializeField] private uint prefabIdentifier;
        [SerializeField] private uint objectIdentifier;

        public uint Identifier => objectIdentifier;

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
            
            var previousId = objectIdentifier;
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(this);
            objectIdentifier = Hashing.GetCRC32Hash(globalId.ToString());
            objectIdentifier &= ~(1u << 31);
            objectType = ENetworkObjectType.Placed;
            
            if (previousId != objectIdentifier)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(this))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (gameObject.scene.name == null || UnityUtilities.IsPrefabInEdit(this))
                return;
#endif
            
            switch (objectType)
            {
                case ENetworkObjectType.Placed:
                    NetworkObjectIdentifications.Instance.RegisterNetworkObjectID(objectIdentifier);
                    break;
                case ENetworkObjectType.Instantiated:
                    objectIdentifier = NetworkObjectIdentifications.Instance.GetNextNetworkObjectID();
                    break;
            }
        }

        /*
        private void OnDestroy()
        {
            NetworkObjectIdentifications.Instance.ReleaseNetworkObjectID(objectIdentifier);
        }
        */
    }
}
