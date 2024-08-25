using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Networking.Transporting
{
    [Serializable]
    public abstract class TransportConfiguration : ScriptableObject
    {
        public TransportSettings Settings;

        public abstract string TransportName { get; }
        public abstract ATransport GetTransport();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TransportConfiguration), true)]
    public class TransportConfigurationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (TransportConfiguration)target;
            
            EditorGUILayout.TextField("Type:", config.TransportName, EditorStyles.label);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Settings"));
            EditorGUI.indentLevel--;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
