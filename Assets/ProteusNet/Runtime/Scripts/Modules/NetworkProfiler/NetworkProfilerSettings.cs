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
        /// <summary>
        /// Where to align the profiler GUI in the visible window.
        /// </summary>
        [field: SerializeField] public ProfilerAlignment Alignment { get; set; } = ProfilerAlignment.TopRight;
        /// <summary>
        /// Whether to show buttons for controlling the network manager in the profiler GUI.
        /// </summary>
        [field: SerializeField] public bool ShowNetworkManagerAPI { get; set; } = true;
        /// <summary>
        /// The color used for the GUI font.
        /// </summary>
        [field: SerializeField] public Color FontColor { get; set; } = new(.56f, .66f, .24f, 1);
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
        private static readonly GUIContent AlignmentDesc = new("Allow Authority Requests", "Where to align the profiler GUI in the visible window.");
        private static readonly GUIContent ShowNetworkManagerAPIDesc = new("Show NetworkManager API", "Whether to show buttons for controlling the network manager in the profiler GUI.");
        private static readonly GUIContent FontColorDesc = new("Font Color", "The color used for the GUI font.");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<Alignment>k__BackingField"), AlignmentDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<ShowNetworkManagerAPI>k__BackingField"), ShowNetworkManagerAPIDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<FontColor>k__BackingField"), FontColorDesc);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
