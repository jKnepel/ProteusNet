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
        [field: SerializeField] public bool UseCompression { get; set; } = true;
        /// <summary>
        /// If compression is active, this will define the number of decimal places to which
        /// floating point numbers will be compressed.
        /// </summary>
        [field: SerializeField] public int NumberOfDecimalPlaces { get; set; } = 3;
        /// <summary>
        /// If compression is active, this will define the number of bits used by the three compressed Quaternion
        /// components in addition to the two flag bits.
        /// </summary>
        [field: SerializeField] public int BitsPerComponent { get; set; } = 10;
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SerializerSettings), true)]
    public class SerializerSettingsDrawer : PropertyDrawer
    {
        private static readonly GUIContent UseCompressionDesc = new("UseCompression", "If compression should be used for all serialisation in the framework.");
        private static readonly GUIContent NumberOfDecimalPlacesDesc = new("Number of Decimal Places:", "If compression is active, this will define the number of decimal places to which floating point numbers will be compressed.");
        private static readonly GUIContent BitsPerComponentDesc = new("Bits per Component:", "If compression is active, this will define the number of bits used by the three compressed Quaternion components in addition to the two flag bits.");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var useCompression = property.FindPropertyRelative("<UseCompression>k__BackingField");
            EditorGUILayout.PropertyField(useCompression, UseCompressionDesc);
            if (useCompression.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<NumberOfDecimalPlaces>k__BackingField"), NumberOfDecimalPlacesDesc);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<BitsPerComponent>k__BackingField"), BitsPerComponentDesc);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
