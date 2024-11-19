using jKnepel.ProteusNet.Utilities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components
{
    [Serializable]
    public class NetworkObjectIdentifications : ScriptableObject
    {
        [SerializeField] private List<NetworkObject> networkObjectPrefabs = new();
        
        private System.Collections.Generic.HashSet<uint> _networkObjectIds = new();

        private static NetworkObjectIdentifications _instance;
        public static NetworkObjectIdentifications Instance
        {
            get
            {
                if (_instance == null)
                    return _instance = UnityUtilities.LoadOrCreateScriptableObject<NetworkObjectIdentifications>("NetworkObjectIdentifications", ProteusSettings.networkIDsDefaultPath);
                return _instance;
            }
        }

        private static ProteusNetSettings _proteusSettings;
        private static ProteusNetSettings ProteusSettings
        {
            get
            {
                if (_proteusSettings == null)
                    _proteusSettings = UnityUtilities.LoadOrCreateScriptableObject<ProteusNetSettings>("ProteusNetSettings");
                return _proteusSettings;
            }
        }

        public uint GetNextNetworkObjectID()
        {
            uint id;
            do
            {
                id = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
                id |= 1u << 31;
            }
            while (!_networkObjectIds.Add(id));
            return id;
        }

        public void RegisterNetworkObjectID(uint networkObjectId)
        {
            var collision = _networkObjectIds.Add(networkObjectId);
            if (!collision)
                Debug.LogError($"An Id-collision has occurred for network objects with the Id {networkObjectId}");
        }
        
        public void ReleaseNetworkObjectID(uint networkObjectId)
        {
            var success = _networkObjectIds.Remove(networkObjectId);
            if (!success)
                Debug.LogError($"The non-existent network object Id {networkObjectId} was attempted to be removed");
        }
        
#if UNITY_EDITOR
        [ContextMenu("Regenerate NetworkObject Prefabs")]
        public void RegenerateNetworkObjectPrefabs()
        {
            // search for all prefabs
            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", ProteusSettings.networkPrefabsSearchPaths);
            var foundPrefabs = new List<NetworkObject>();
            foreach (var prefabGUID in prefabGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && prefab.TryGetComponent<NetworkObject>(out var networkObject))
                    foundPrefabs.Add(networkObject);
            }
            
            // set prefabs to asset
            networkObjectPrefabs = foundPrefabs;
            networkObjectPrefabs.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.Ordinal));
            EditorUtility.SetDirty(this);
            
            // update identification in instances
            for (var i = 0; i < networkObjectPrefabs.Count; i++)
            {
                var networkObject = networkObjectPrefabs[i];
                var serializedNetworkObject = new SerializedObject(networkObject);
                serializedNetworkObject.FindProperty("prefabIdentifier").intValue = i;
                serializedNetworkObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(networkObject.gameObject);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("NetworkObject prefab collection was regenerated!");
        }
#endif
    }
}
