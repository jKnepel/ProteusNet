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
                EditorGUILayout.PropertyField(property.FindPropertyRelative("Alignment"), new GUIContent("Alignment"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ShowNetworkManagerAPI"), new GUIContent("Show NetworkManager API"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("FontColor"), new GUIContent("Font Color"));
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
