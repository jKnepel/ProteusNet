using jKnepel.ProteusNet.Components;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    [RequireComponent(typeof(MonoNetworkManager))]
    [AddComponentMenu("ProteusNet/Modules/Server Discovery (Module)")]
    public class ServerDiscoveryConfiguration : AModuleConfigurationComponent<ServerDiscoveryModule>
    {
        protected override ServerDiscoveryModule CreateInstance() => new();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ServerDiscoveryConfiguration), true)]
    public class ServerDiscoveryConfigurationEditor : Editor
    {
        private SerializedProperty _valueProperty;

        private void OnEnable()
        {
            _valueProperty = serializedObject.FindProperty("value");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_valueProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
