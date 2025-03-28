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
        private SerializedProperty _distributedAuthorityProp;
        private SerializedProperty _allowAuthorityRequestsProp;
        private SerializedProperty _objectTypeProp;
        private SerializedProperty _objectIdentifierProp;
        private SerializedProperty _prefabIdentifierProp;

        private void OnEnable()
        {
            _networkManagerProp = serializedObject.FindProperty("networkManager");
            _distributedAuthorityProp = serializedObject.FindProperty("distributedAuthority");
            _allowAuthorityRequestsProp = serializedObject.FindProperty("allowAuthorityRequests");
            _objectTypeProp = serializedObject.FindProperty("objectType");
            _objectIdentifierProp = serializedObject.FindProperty("objectIdentifier");
            _prefabIdentifierProp = serializedObject.FindProperty("prefabIdentifier");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var networkObject = (NetworkObject)target;

            EditorGUILayout.PropertyField(_networkManagerProp, new GUIContent("Network Manager"));
            
            EditorGUILayout.PropertyField(_allowAuthorityRequestsProp, new GUIContent("Allow Authority Requests"));
            EditorGUILayout.PropertyField(_distributedAuthorityProp, new GUIContent("Distributed Authority"));

            EditorGUILayout.Space();
            
            _showInfoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showInfoFoldout, new GUIContent("Info"));
            if (_showInfoFoldout)
            {
                EditorGUI.indentLevel++;
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(_objectTypeProp, new GUIContent("Object Type"));
                    EditorGUILayout.PropertyField(_objectIdentifierProp, new GUIContent("Object Identifier"));
                    EditorGUILayout.PropertyField(_prefabIdentifierProp, new GUIContent("Prefab Identifier"));
                    EditorGUILayout.Toggle(new GUIContent("Is Spawned"), networkObject.IsSpawned);
                    
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
                
                // DEBUG
                IndentedButton("Assign Authority", () => networkObject.AssignAuthority(1));
                IndentedButton("Remove Authority", () => networkObject.RemoveAuthority());
                IndentedButton("Assign Ownership", () => networkObject.AssignOwnership(1));
                IndentedButton("Remove Ownership", () => networkObject.RemoveOwnership());
                if (!networkObject.IsAuthor)
                    IndentedButton("Request Authority", () => networkObject.RequestAuthority());
                else
                    IndentedButton("Release Authority", () => networkObject.ReleaseAuthority());
                if (!networkObject.IsOwner)
                    IndentedButton("Request Ownership", () => networkObject.RequestOwnership());
                else
                    IndentedButton("Release Ownership", () => networkObject.ReleaseOwnership());
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void IndentedButton(string label, System.Action onClick)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15); // Indent by the current level
            if (GUILayout.Button(label)) onClick?.Invoke();
            GUILayout.EndHorizontal();
        }
    }
}
