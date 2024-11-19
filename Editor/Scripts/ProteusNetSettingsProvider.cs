using jKnepel.ProteusNet.Utilities;
using UnityEngine;
using UnityEditor;

namespace jKnepel.ProteusNet
{
    internal static class ProteusNetSettingsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateProteusNetSettingsProvider()
        {
            var provider = new SettingsProvider("Project/ProteusNetSettings", SettingsScope.Project)
            {
                label = "ProteusNet",
                guiHandler = _ =>
                {
                    // TODO : beautify
                    SerializedObject settings = new(UnityUtilities.LoadOrCreateScriptableObject<ProteusNetSettings>("ProteusNetSettings"));
                    EditorGUILayout.PropertyField(settings.FindProperty("networkIDsDefaultPath"), new GUIContent("NetworkObject Default Path", "Where the NetworkObjectPrefabs asset will be saved once generated."));
                    EditorGUILayout.PropertyField(settings.FindProperty("networkPrefabsSearchPaths"), new GUIContent("NetworkObject Prefabs Search Paths", "In which directory NetworkObject prefabs will be searched for. Leave empty if all directory should be searched."));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "NetworkObjects"
                })
            };

            return provider;
        }
    }
}
