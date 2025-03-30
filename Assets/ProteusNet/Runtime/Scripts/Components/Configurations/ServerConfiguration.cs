using jKnepel.ProteusNet.Networking;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using jKnepel.ProteusNet.Utilities;
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Components
{
    [AddComponentMenu("Server Configuration")]
    public class ServerConfiguration : AConfigurationComponent<Server>
    {
        protected override Server CreateInstance() => new();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ServerConfiguration))]
    public class ServerConfigurationEditor : Editor
    {
        private readonly GUIStyle _style = new();
        private Vector2 _viewPos;
        
        private SavedBool _showClientsList;

        private void OnEnable()
        {
            var server = ((ServerConfiguration)target).Value;
            server.OnLocalStateUpdated += RepaintOnUpdate;
            server.OnRemoteClientConnected += RepaintOnUpdate;
            server.OnRemoteClientDisconnected += RepaintOnUpdate;
            server.OnRemoteClientUpdated += RepaintOnUpdate;
            server.OnServerUpdated += RepaintOnUpdate;
            
            _showClientsList = new($"{target.GetType()}.ShowClientsList", false);
        }
        
        private void OnDestroy()
        {
            var server = ((ServerConfiguration)target).Value;
            server.OnLocalStateUpdated -= RepaintOnUpdate;
            server.OnRemoteClientConnected -= RepaintOnUpdate;
            server.OnRemoteClientDisconnected -= RepaintOnUpdate;
            server.OnRemoteClientUpdated -= RepaintOnUpdate;
            server.OnServerUpdated -= RepaintOnUpdate;
        }
        
        private void RepaintOnUpdate() => Repaint();
        private void RepaintOnUpdate(uint _) => Repaint();
        private void RepaintOnUpdate(ELocalServerConnectionState _) => Repaint();
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var server = ((ServerConfiguration)target).Value;
            
            server.Servername = EditorGUILayout.TextField(new GUIContent("Servername"), server.Servername);
            
            if (!server.IsActive)
            {
                if (GUILayout.Button(new GUIContent("Start Server")))
                    server.Start();
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Stop Server")))
                    server.Stop();
                
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    _showClientsList.Value = EditorGUILayout.Foldout(_showClientsList.Value, new GUIContent("Connected Clients"), true);
                    EditorGUILayout.LabelField($"{server.NumberOfConnectedClients}/{server.MaxNumberOfClients}");
                }

                if (_showClientsList)
                {
                    using (new GUILayout.ScrollViewScope(_viewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150)))
                    {
                        if (server.NumberOfConnectedClients == 0)
                        {
                            GUILayout.Label($"There are no clients connected to the local server!");
                            return;
                        }

                        var defaultColour = _style.normal.textColor;
                        _style.alignment = TextAnchor.MiddleCenter;
                        for (var i = 0; i < server.NumberOfConnectedClients; i++)
                        {
                            var client = server.ConnectedClients.Values.ElementAt(i);
                            EditorGUILayout.BeginHorizontal();
                            _style.normal.textColor = client.UserColour;
                            GUILayout.Label($"#{client.ID} {client.Username}", _style);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Kick Client"))
                                server.DisconnectClient(client.ID);
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
