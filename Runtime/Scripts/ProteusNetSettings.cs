using jKnepel.ProteusNet.Utilities;
using System;
using UnityEngine;

namespace jKnepel.ProteusNet
{
    [Serializable]
    public class ProteusNetSettings : ScriptableObject
    {
        private static ProteusNetSettings _instance;
        public static ProteusNetSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = UnityUtilities.LoadOrCreateScriptableObject<ProteusNetSettings>("ProteusNetSettings");
                return _instance;
            }
        }
        
        [SerializeField, HideInInspector] public string networkIDsDefaultPath = "Assets/";
        [SerializeField, HideInInspector] public string[] networkPrefabsSearchPaths;
    }
}
