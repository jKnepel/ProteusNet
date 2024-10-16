using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Serializing
{
    [Serializable]
    public class SerializerSettings
    {
        /// <summary>
        /// If compression should be used for all serialisation in the framework.
        /// </summary>
        public bool UseCompression = true;
        /// <summary>
        /// If compression is active, this will define the number of decimal places to which
        /// floating point numbers will be compressed.
        /// </summary>
        public int NumberOfDecimalPlaces = 3;
        /// <summary>
        /// If compression is active, this will define the number of bits used by the three compressed Quaternion
        /// components in addition to the two flag bits.
        /// </summary>
        public int BitsPerComponent = 10;
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SerializerSettings), true)]
    public class SerializerSettingsDrawer : PropertyDrawer
    {
        private bool _areSettingsVisible;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _areSettingsVisible = EditorGUILayout.Foldout(_areSettingsVisible, "Settings", true);
            if (_areSettingsVisible)
            {
                var useCompression = property.FindPropertyRelative("UseCompression");
                EditorGUILayout.PropertyField(useCompression, new GUIContent("UseCompression", "If compression should be used for all serialisation in the framework."));
                if (useCompression.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("NumberOfDecimalPlaces"), new GUIContent("Number of Decimal Places:", "If compression is active, this will define the number of decimal places to which floating point numbers will be compressed."));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("BitsPerComponent"), new GUIContent("Bits per Component:", "If compression is active, this will define the number of bits used by the three compressed Quaternion components in addition to the two flag bits."));
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
