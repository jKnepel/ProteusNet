using jKnepel.ProteusNet.Networking;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using jKnepel.ProteusNet.Utilities;
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components.Configuration
{
    [AddComponentMenu("ProteusNet/Configuration/Client Configuration")]
    public class ClientConfiguration : AConfigurationComponent<Client>
    {
        protected override Client CreateInstance() => new();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ClientConfiguration))]
    public class ClientConfigurationEditor : Editor
    {
        private readonly GUIStyle _style = new();
        private Vector2 _viewPos;

        private SavedBool _showClientsList;

        private void OnEnable()
        {
            var client = ((ClientConfiguration)target).Value;
            client.OnLocalStateUpdated += RepaintOnUpdate;
            client.OnRemoteClientConnected += RepaintOnUpdate;
            client.OnRemoteClientDisconnected += RepaintOnUpdate;
            client.OnRemoteClientUpdated += RepaintOnUpdate;
            client.OnServerUpdated += RepaintOnUpdate;
            
            _showClientsList = new($"{target.GetType()}.ShowClientsList", false);
        }
        
        private void OnDestroy()
        {
            var client = ((ClientConfiguration)target).Value;
            client.OnLocalStateUpdated -= RepaintOnUpdate;
            client.OnRemoteClientConnected -= RepaintOnUpdate;
            client.OnRemoteClientDisconnected -= RepaintOnUpdate;
            client.OnRemoteClientUpdated -= RepaintOnUpdate;
            client.OnServerUpdated -= RepaintOnUpdate;
        }
        
        private void RepaintOnUpdate() => Repaint();
        private void RepaintOnUpdate(uint _) => Repaint();
        private void RepaintOnUpdate(ELocalClientConnectionState _) => Repaint();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var client = ((ClientConfiguration)target).Value;
            
            client.Username = EditorGUILayout.TextField(new GUIContent("Username"), client.Username);
            client.UserColour = EditorGUILayout.ColorField(new GUIContent("User colour"), client.UserColour);
            
            if (!client.IsActive)
            {
                if (GUILayout.Button(new GUIContent("Start Client")))
                    client.Start();
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Stop Client")))
                    client.Stop();
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("ID", $"{client.ClientID}");
                EditorGUILayout.LabelField("Servername", client.Servername);

                _showClientsList.Value = EditorGUILayout.Foldout(_showClientsList.Value, new GUIContent($"Connected Clients {client.NumberOfConnectedClients}/{client.MaxNumberOfClients}"), true);
                if (_showClientsList)
                {
                    using (new GUILayout.ScrollViewScope(_viewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150)))
                    {
                        if (client.ConnectedClients.Count == 0)
                        {
                            GUILayout.Label($"There are no other clients connected to the server!");
                            return;
                        }
                
                        var defaultColour = _style.normal.textColor;
                        _style.alignment = TextAnchor.MiddleLeft;
                        for (var i = 0; i < client.ConnectedClients.Count; i++)
                        {
                            var clientI = client.ConnectedClients.Values.ElementAt(i);
                            EditorGUILayout.BeginHorizontal();
                            _style.normal.textColor = client.UserColour;
                            GUILayout.Label($"#{clientI.ID} {client.Username}", _style);
                            EditorGUILayout.EndHorizontal();
                        }
                        _style.normal.textColor = defaultColour;
                    }
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
