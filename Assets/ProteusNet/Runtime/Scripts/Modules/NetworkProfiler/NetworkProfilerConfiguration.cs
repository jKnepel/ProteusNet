using jKnepel.ProteusNet.Components;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.NetworkProfiler
{
    [RequireComponent(typeof(MonoNetworkManager))]
    [AddComponentMenu("ProteusNet/Modules/Network Profiler (Module)")]
    public class NetworkProfilerConfiguration : AModuleConfigurationComponent<NetworkProfilerModule>
    {
        protected override NetworkProfilerModule CreateInstance() => new();

        private new void Awake()
        {
            base.Awake();
            Value.NetworkManager.OnTickCompleted += Value.OnTickComplete;
        }

        private void Update()
        {
            Value.Update();
        }

        private void OnGUI()
        {
            Value.GUI();
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkProfilerConfiguration), true)]
    public class NetworkProfilerConfigurationEditor : Editor
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
