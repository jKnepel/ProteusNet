using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkObject), true)]
    public class NetworkObjectEditor : Editor
    {
        private bool _showInfoFoldout = true;

        private SerializedProperty _objectTypeProp;
        private SerializedProperty _objectIdentifierProp;
        private SerializedProperty _prefabIdentifierProp;
        private SerializedProperty _networkManagerProp;

        private void OnEnable()
        {
            _objectTypeProp = serializedObject.FindProperty("objectType");
            _objectIdentifierProp = serializedObject.FindProperty("objectIdentifier");
            _prefabIdentifierProp = serializedObject.FindProperty("prefabIdentifier");
            _networkManagerProp = serializedObject.FindProperty("networkManager");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var networkObject = (NetworkObject)target;

            _showInfoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showInfoFoldout, new GUIContent("Debug Info"));
            if (_showInfoFoldout)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(_objectTypeProp, new GUIContent("Object Type"));
                    EditorGUILayout.PropertyField(_objectIdentifierProp, new GUIContent("Object Identifier"));
                    EditorGUILayout.PropertyField(_prefabIdentifierProp, new GUIContent("Prefab Identifier"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.PropertyField(_networkManagerProp, new GUIContent("Network Manager"));
            EditorGUILayout.Toggle(new GUIContent("Is Spawned"), networkObject.IsSpawned);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
