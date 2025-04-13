using jKnepel.ProteusNet.Networking.Transporting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components.Configuration
{
    [AddComponentMenu("ProteusNet/Configuration/Unity Transport Configuration")]
    public class UnityTransportConfiguration : AConfigurationComponent<ATransport>
    {
        protected override ATransport CreateInstance() => new UnityTransport();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(UnityTransportConfiguration))]
    public class UnityTransportConfigurationEditor : Editor
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
