using System;
using UnityEngine;

namespace jKnepel.ProteusNet
{
    [Serializable]
    public class ProteusNetSettings : ScriptableObject
    {
        [SerializeField, HideInInspector] public string networkIDsDefaultPath = "Assets/";
        [SerializeField, HideInInspector] public string[] networkPrefabsSearchPaths;
    }
}
