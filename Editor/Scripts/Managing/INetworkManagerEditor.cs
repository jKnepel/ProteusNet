using jKnepel.ProteusNet.Modules;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace jKnepel.ProteusNet.Managing
{
    internal class INetworkManagerEditor
    {
        #region fields

        private readonly INetworkManager _manager;

        private readonly GUIStyle _style = new();

        private bool _showModuleWindow = true;
        private ModuleConfiguration _moduleConfig;
        private Vector2 _modulePos;
        
        private bool _showServerWindow = true;
        private Vector2 _serverClientsViewPos;

        private bool _showClientWindow = true;
        private Vector2 _clientClientsViewPos;

        #endregion

        #region lifecycle

        public INetworkManagerEditor(INetworkManager manager)
        {
            _manager = manager;
        }

        #endregion

        #region guis
        
        public T ConfigurationGUI<T>(ScriptableObject configuration, string title, ref bool showSection) where T : ScriptableObject
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout(title, ref showSection);
            if (showSection)
            {
                configuration = (T)EditorGUILayout.ObjectField("Asset", configuration, typeof(T), false);

                if (configuration)
                    Editor.CreateEditor(configuration).OnInspectorGUI();
            }
            GUILayout.EndVertical();

            return configuration as T;
        }
        
        public void ModuleGUI()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawToggleFoldout("Modules", ref _showModuleWindow);
                if (!_showModuleWindow || _manager.Modules == null) return;
                
                GUILayout.BeginHorizontal();
                _moduleConfig = (ModuleConfiguration)EditorGUILayout.ObjectField(_moduleConfig, typeof(ModuleConfiguration), false);
                if (GUILayout.Button("Add Module") && _moduleConfig is not null)
                    _manager.Modules.Add(_moduleConfig.GetModule(_manager));
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
                            
                foreach (var module in _manager.Modules.ToList())
                    module.RenderModuleGUI(() => RemoveModule(module));
                if (_manager.Modules.Count > 0)
                    EditorGUILayout.Space(3);
            }

            return;
            void RemoveModule(Module module)
            {
                _manager.Modules.Remove(module);
                module.Dispose();
            }
        }
        
        public void ServerGUI()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawToggleFoldout("Server", ref _showServerWindow, _manager.IsServer, "Is Server");
                if (!_showServerWindow) return;
                
                if (!_manager.IsServer)
                {
                    _manager.Server.Servername = EditorGUILayout.TextField(new GUIContent("Servername"), _manager.Server.Servername);
                    if (GUILayout.Button(new GUIContent("Start Server")) && AllowStart())
                        _manager.StartServer();
                    return;
                }
                
                _manager.Server.Servername = EditorGUILayout.TextField("Servername", _manager.Server.Servername);
                EditorGUILayout.LabelField("Connected Clients", $"{_manager.Server.NumberOfConnectedClients}/{_manager.Server.MaxNumberOfClients}");
                if (GUILayout.Button(new GUIContent("Stop Server")))
                    _manager.StopServer();

                using (new GUILayout.ScrollViewScope(_serverClientsViewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150)))
                {
                    if (_manager.Server.NumberOfConnectedClients == 0)
                    {
                        GUILayout.Label($"There are no clients connected to the local server!");
                        return;
                    }
                    
                    var defaultColour = _style.normal.textColor;
                    _style.alignment = TextAnchor.MiddleCenter;
                    for (var i = 0; i < _manager.Server.NumberOfConnectedClients; i++)
                    {
                        var client = _manager.Server.ConnectedClients.Values.ElementAt(i);
                        EditorGUILayout.BeginHorizontal();
                        _style.normal.textColor = client.UserColour;
                        GUILayout.Label($"#{client.ID} {client.Username}", _style);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Kick Client"))
                            _manager.Server.DisconnectClient(client.ID);
                        EditorGUILayout.EndHorizontal();
                    }

                    _style.normal.textColor = defaultColour;
                }
            }
        }

        public void ClientGUI()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawToggleFoldout("Client", ref _showClientWindow, _manager.IsClient, "Is Client");
                if (!_showClientWindow) return;
                
                if (!_manager.IsClient)
                {
                    _manager.Client.Username = EditorGUILayout.TextField(new GUIContent("Username"), _manager.Client.Username);
                    _manager.Client.UserColour = EditorGUILayout.ColorField(new GUIContent("User colour"), _manager.Client.UserColour);
                    if (GUILayout.Button(new GUIContent("Start Client")) && AllowStart())
                        _manager.StartClient();
                    return;
                }
                
                EditorGUILayout.LabelField("ID", $"{_manager.Client.ClientID}");
                _manager.Client.Username = EditorGUILayout.TextField("Username", _manager.Client.Username);
                _manager.Client.UserColour = EditorGUILayout.ColorField("User colour", _manager.Client.UserColour);
                EditorGUILayout.LabelField("Servername", _manager.Client.Servername);
                EditorGUILayout.LabelField("Connected Clients", $"{_manager.Client.NumberOfConnectedClients}/{_manager.Client.MaxNumberOfClients}");
                if (GUILayout.Button(new GUIContent("Stop Client")))
                    _manager.StopClient();

                using (new GUILayout.ScrollViewScope(_clientClientsViewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150)))
                {
                    if (_manager.Client.ConnectedClients.Count == 0)
                    {
                        GUILayout.Label($"There are no other clients connected to the server!");
                        return;
                    }
                    
                    var defaultColour = _style.normal.textColor;
                    _style.alignment = TextAnchor.MiddleLeft;
                    for (var i = 0; i < _manager.Client.ConnectedClients.Count; i++)
                    {
                        var client = _manager.Client.ConnectedClients.Values.ElementAt(i);
                        EditorGUILayout.BeginHorizontal();
                        _style.normal.textColor = client.UserColour;
                        GUILayout.Label($"#{client.ID} {client.Username}", _style);
                        EditorGUILayout.EndHorizontal();
                    }
                    _style.normal.textColor = defaultColour;
                }
            }
        }

        #endregion

        #region utilities

        private bool AllowStart()
        {
            return _manager.ManagerScope switch
            {
                EManagerScope.Runtime => EditorApplication.isPlaying,
                EManagerScope.Editor => !EditorApplication.isPlaying,
                _ => false
            };
        }
        
        private static void DrawToggleFoldout(string title, ref bool isExpanded,
            bool? checkbox = null, string checkboxLabel = null)
        {
            Color normalColour = new(0.24f, 0.24f, 0.24f);
            Color hoverColour = new(0.27f, 0.27f, 0.27f);
            var currentColour = normalColour;

            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 2f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x = backgroundRect.width - 7f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            var toggleLabelRect = backgroundRect;
            toggleLabelRect.x = -10f;

            var e = Event.current;
            if (labelRect.Contains(e.mousePosition))
                currentColour = hoverColour;
            EditorGUI.DrawRect(backgroundRect, currentColour);

            if (isExpanded)
            {
                var borderBot = GUILayoutUtility.GetRect(1f, 0.6f);
                EditorGUI.DrawRect(borderBot, new(0, 0, 0));
            }

            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            isExpanded = GUI.Toggle(foldoutRect, isExpanded, GUIContent.none, EditorStyles.foldout);

            if (checkbox is not null)
            {
                if (checkboxLabel is not null)
                {
                    var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
                    EditorGUI.LabelField(toggleLabelRect, checkboxLabel, labelStyle);
                }
                EditorGUI.Toggle(toggleRect, (bool)checkbox, new("ShurikenToggle"));
            }

            if (e.type == EventType.MouseDown && labelRect.Contains(e.mousePosition) && e.button == 0)
            {
                isExpanded = !isExpanded;
                e.Use();
            }
        }

        #endregion
    }
}
