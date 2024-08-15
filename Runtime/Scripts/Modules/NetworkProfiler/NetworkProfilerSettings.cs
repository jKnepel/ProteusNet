using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.NetworkProfiler
{
    [Serializable]
    public class NetworkProfilerSettings
    {
        public ProfilerAlignment Alignment = ProfilerAlignment.TopRight;
        public bool ShowNetworkManagerAPI = true;
        public Color FontColor = new(.56f, .66f, .24f, 1);
        public string ProfilerFilePath = string.Empty;
        public string ClientProfileFileName = "clientNetworkStatistics.csv";
        public string ServerProfileFileName = "serverNetworkStatistics.csv";
    }

    public enum ProfilerAlignment
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(NetworkProfilerSettings), true)]
    public class NetworkProfilerSettingsDrawer : PropertyDrawer
    {
        private bool _areSettingsVisible;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _areSettingsVisible = EditorGUILayout.Foldout(_areSettingsVisible, "Settings", true);
            if (_areSettingsVisible)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("Alignment"), new GUIContent("Alignment", "Where to align the profiler GUI in the visible window."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ShowNetworkManagerAPI"), new GUIContent("Show NetworkManager API", "Whether to show buttons for controlling the network manager in the profiler GUI."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("FontColor"), new GUIContent("Font Color", "The color used for the GUI font."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ProfilerFilePath"), new GUIContent("Profiler Filepath", "The path to which network traffic statistics will be exported. If left empty, the default Unity persistent data path will be used."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ClientProfileFileName"), new GUIContent("Client Filename", "The filename used for the client traffic statistics."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ServerProfileFileName"), new GUIContent("Server Filename", "The filename used for the server traffic statistics."));
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
