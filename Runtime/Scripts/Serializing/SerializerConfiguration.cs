using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Serializing
{
	[CreateAssetMenu(fileName = "SerializerConfiguration", menuName = "ProteusNet/SerializerConfiguration")]
    public class SerializerConfiguration : ScriptableObject
    {
	    public SerializerSettings Settings = new();
    }

#if UNITY_EDITOR
	[CustomEditor(typeof(SerializerConfiguration), true)]
	public class SerializerConfigurationEditor : Editor
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
