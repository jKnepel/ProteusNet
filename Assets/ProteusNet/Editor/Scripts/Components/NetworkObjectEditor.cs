using jKnepel.ProteusNet.Utilities;
using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkObject), true)]
    public class NetworkObjectEditor : Editor
    {
        private SerializedProperty _networkManager;
        private SerializedProperty _distributedAuthority;
        private SerializedProperty _allowAuthorityRequests;
        private SerializedProperty _objectType;
        private SerializedProperty _objectIdentifier;
        private SerializedProperty _prefabIdentifier;
        
        private SavedBool _showInfoFoldout;

        private readonly GUIContent _allowAuthRequestsDesc = new("Allow Authority Requests", "Allows clients to request authority and ownership over the network object. Requests are automatically managed by the server.");
        private readonly GUIContent _distributedAuthDesc = new("Distributed Authority", "Enables a distributed authority model, in which clients with authority are responsible for replicating network object updates to the network.");

        private void OnEnable()
        {
            _networkManager = serializedObject.FindProperty("networkManager");
            _distributedAuthority = serializedObject.FindProperty("distributedAuthority");
            _allowAuthorityRequests = serializedObject.FindProperty("allowAuthorityRequests");
            _objectType = serializedObject.FindProperty("objectType");
            _objectIdentifier = serializedObject.FindProperty("objectIdentifier");
            _prefabIdentifier = serializedObject.FindProperty("prefabIdentifier");
            
            _showInfoFoldout = new($"{target.GetType()}.ShowInfoFoldout", false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var networkObject = (NetworkObject)target;

            EditorGUILayout.PropertyField(_networkManager, new GUIContent("Network Manager"));
            
            EditorGUILayout.PropertyField(_allowAuthorityRequests, _allowAuthRequestsDesc);
            EditorGUILayout.PropertyField(_distributedAuthority, _distributedAuthDesc);

            EditorGUILayout.Space();
            
            _showInfoFoldout.Value = EditorGUILayout.BeginFoldoutHeaderGroup(_showInfoFoldout.Value, new GUIContent("Info"));
            if (_showInfoFoldout)
            {
                EditorGUI.indentLevel++;
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(_objectType, new GUIContent("Object Type"));
                    EditorGUILayout.PropertyField(_objectIdentifier, new GUIContent("Object Identifier"));
                    EditorGUILayout.PropertyField(_prefabIdentifier, new GUIContent("Prefab Identifier"));
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
                
                /*
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
                */
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void IndentedButton(string label, System.Action onClick)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            if (GUILayout.Button(label)) onClick?.Invoke();
            GUILayout.EndHorizontal();
        }
    }
}
