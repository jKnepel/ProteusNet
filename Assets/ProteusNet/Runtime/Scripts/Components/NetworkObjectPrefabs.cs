using jKnepel.ProteusNet.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components
{
    [Serializable]
    public class NetworkObjectPrefabs : ScriptableObject
    {
        [SerializeField] private List<NetworkObject> networkObjectPrefabs = new();

        public NetworkObject this[uint i] => networkObjectPrefabs[(int)i];
        public NetworkObject this[int i] => networkObjectPrefabs[i];

        public bool TryGet(uint i, out NetworkObject networkObject) => TryGet((int)i, out networkObject);
        public bool TryGet(int i, out NetworkObject networkObject)
        {
            if (i < 0 || i >= networkObjectPrefabs.Count)
            {
                networkObject = null;
                return false;
            }

            networkObject = networkObjectPrefabs[i];
            return true;
        }
        
        private static NetworkObjectPrefabs _instance;
        public static NetworkObjectPrefabs Instance
        {
            get
            {
                if (_instance == null)
                    return _instance = UnityUtilities.LoadOrCreateScriptableObject<NetworkObjectPrefabs>("NetworkObjectPrefabs", ProteusNetSettings.Instance.networkIDsDefaultPath);
                return _instance;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate NetworkObject Prefabs")]
        public void RegenerateNetworkObjectPrefabs()
        {
            // search for all prefabs
            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", ProteusNetSettings.Instance.networkPrefabsSearchPaths);
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
