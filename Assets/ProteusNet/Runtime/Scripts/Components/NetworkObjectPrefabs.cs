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
        [SerializeField] private List<string> searchPaths = new() { "Assets" };
        [SerializeField] private List<NetworkObject> networkObjectPrefabs = new();

        public NetworkObject this[uint i] => networkObjectPrefabs[(int)i];
        public NetworkObject this[int i] => networkObjectPrefabs[i];

        public bool TryFindPrefab(NetworkObject networkObject, out int i)
        {
            var found = false;
            for (i = 0; i < networkObjectPrefabs.Count; i++)
            {
                if (networkObject.PrefabIdentifier != networkObjectPrefabs[i].ObjectIdentifier) continue;
                found = true;
                break;
            }

            if (!found) i = -1;
            return found;
        }
        
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
    }
}
