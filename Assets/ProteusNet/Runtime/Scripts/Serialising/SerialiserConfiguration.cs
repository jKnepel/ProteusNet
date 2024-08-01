using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Serialising
{
	[CreateAssetMenu(fileName = "SerialiserConfiguration", menuName = "ProteusNet/SerialiserConfiguration")]
    public class SerialiserConfiguration : ScriptableObject
    {
	    public SerialiserSettings Settings = new();
    }

#if UNITY_EDITOR
	[CustomEditor(typeof(SerialiserConfiguration), true)]
	public class SerialiserConfigurationEditor : Editor
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
