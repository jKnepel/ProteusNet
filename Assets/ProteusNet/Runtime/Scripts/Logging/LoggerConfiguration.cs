using jKnepel.ProteusNet.Components.Configuration;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Logging
{
    [CreateAssetMenu(fileName = "LoggerConfiguration", menuName = "ProteusNet/LoggerConfiguration")]
    public class LoggerConfiguration : AConfigurationAsset<Logger>
    {
        protected override Logger CreateInstance() => new();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(LoggerConfiguration))]
    public class LoggerConfigurationEditor : Editor
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
