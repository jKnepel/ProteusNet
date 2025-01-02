using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkObject), true)]
    public class NetworkObjectEditor : Editor
    {
        private bool _showInfoFoldout = true;

        private SerializedProperty _networkManagerProp;
        private SerializedProperty _hasDistributedAuthorityProp;
        private SerializedProperty _objectTypeProp;
        private SerializedProperty _objectIdentifierProp;
        private SerializedProperty _prefabIdentifierProp;

        private void OnEnable()
        {
            _networkManagerProp = serializedObject.FindProperty("networkManager");
            _hasDistributedAuthorityProp = serializedObject.FindProperty("hasDistributedAuthority");
            _objectTypeProp = serializedObject.FindProperty("objectType");
            _objectIdentifierProp = serializedObject.FindProperty("objectIdentifier");
            _prefabIdentifierProp = serializedObject.FindProperty("prefabIdentifier");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var networkObject = (NetworkObject)target;

            EditorGUILayout.PropertyField(_networkManagerProp, new GUIContent("Network Manager"));
            
            EditorGUILayout.PropertyField(_hasDistributedAuthorityProp, new GUIContent("Distributed Authority"));
            if (_hasDistributedAuthorityProp.boolValue)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(true))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.IntField(new GUIContent("Author ID"), (int)networkObject.AuthorID);
                        EditorGUILayout.Space();
                        GUILayout.Label(new GUIContent("Is Author"));
                        EditorGUILayout.Toggle(networkObject.IsAuthor);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.IntField(new GUIContent("Owner ID"), (int)networkObject.OwnerID);
                        EditorGUILayout.Space();
                        GUILayout.Label(new GUIContent("Is Owner"));
                        EditorGUILayout.Toggle(networkObject.IsOwner);
                    }
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_authoritySequence"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_ownershipSequence"));
                
                if (GUILayout.Button("Give Authority"))
                    networkObject.AssignAuthority(1);
                if (GUILayout.Button("Remove Authority"))
                    networkObject.RemoveAuthority();
                if (!networkObject.IsAuthor && GUILayout.Button("Request Authority"))
                    networkObject.RequestAuthority();
                if (networkObject.IsAuthor && GUILayout.Button("Release Authority"))
                    networkObject.ReleaseAuthority();
                if (!networkObject.IsOwner && GUILayout.Button("Request Ownership"))
                    networkObject.RequestOwnership();
                if (networkObject.IsOwner && GUILayout.Button("Release Ownership"))
                    networkObject.ReleaseOwnership();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Toggle(new GUIContent("Is Spawned"), networkObject.IsSpawned);
            
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
