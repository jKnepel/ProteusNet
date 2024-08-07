using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Logging
{
    [CreateAssetMenu(fileName = "LoggerConfiguration", menuName = "ProteusNet/LoggerConfiguration")]
    public class LoggerConfiguration : ScriptableObject
    {
        public LoggerSettings Settings = new();

        public Logger GetLogger()
        {
            return new(Settings);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(LoggerConfiguration), true)]
    public class LoggerConfigurationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Settings"));
            EditorGUI.indentLevel--;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
