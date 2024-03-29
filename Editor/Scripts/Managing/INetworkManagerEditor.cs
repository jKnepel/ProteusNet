using jKnepel.SimpleUnityNetworking.Logging;
using jKnepel.SimpleUnityNetworking.Serialising;
using jKnepel.SimpleUnityNetworking.Transporting;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace jKnepel.SimpleUnityNetworking.Managing
{
    internal class INetworkManagerEditor
    {
        public enum EAllowStart
        {
            Anywhere,
            OnlyEditor,
            OnlyPlaymode
        }
        
        #region fields
        
        private readonly INetworkManager _manager;
        private readonly Action _repaint;
        private readonly EAllowStart _allowStart;

        private readonly GUIStyle _style = new();
        
        private bool _showTransportWindow;
        private bool _showSerialiserWindow;
        private bool _showLoggerWindow;
        private bool _showLoggerMessages;
        
        private bool _showServerWindow;
        private string _servername = "New Server";
        private Vector2 _serverClientsViewPos;

        private bool _showClientWindow;
        private string _username = "Username";
        private Color32 _userColour = new(153, 191, 97, 255);
        private Vector2 _clientClientsViewPos;

        #endregion
        
        #region lifecycle

        public INetworkManagerEditor(INetworkManager manager, Action repaint, EAllowStart allowStart)
        {
            _manager = manager;
            _repaint = repaint;
            _allowStart = allowStart;
        }

        public void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Configurations:", EditorStyles.boldLabel);
            {
                TransportGUI();
                SerialiserGUI();
                LoggerGUI();
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("Managers:", EditorStyles.boldLabel);
            {
                ServerGUI();
                ClientGUI();
            }
        }
        
        #endregion
        
        #region configs
        
        private void TransportGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout("Transport:", ref _showTransportWindow);
            if (_showTransportWindow)
            {
                _manager.TransportConfiguration = (TransportConfiguration)EditorGUILayout.ObjectField(
                    "Transport Configuration:",
                    _manager.TransportConfiguration,
                    typeof(TransportConfiguration),
                    false
                );
                if (_manager.TransportConfiguration)
                    Editor.CreateEditor(_manager.TransportConfiguration).OnInspectorGUI();
            }
            GUILayout.EndVertical();
        }

        private void SerialiserGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout("Serialiser:", ref _showSerialiserWindow);
            if (_showSerialiserWindow && _manager.SerialiserConfiguration != null)
            {
                _manager.SerialiserConfiguration.UseCompression = (EUseCompression)EditorGUILayout.EnumPopup(
                    new GUIContent("Use Compression:", "If, and what kind of compression should be used for all serialisation in the framework."),
                    _manager.SerialiserConfiguration.UseCompression
                );
                _manager.SerialiserConfiguration.NumberOfDecimalPlaces = EditorGUILayout.IntField(
                    new GUIContent("Number of Decimal Places:", "If compression is active, this will define the number of decimal places to which floating point numbers will be compressed."),
                    _manager.SerialiserConfiguration.NumberOfDecimalPlaces
                );
                _manager.SerialiserConfiguration.BitsPerComponent = EditorGUILayout.IntField(
                    new GUIContent("Bits Per Quaternion Component:", "If compression is active, this will define the number of bits used by the three compressed Quaternion components in addition to the two flag bits."),
                    _manager.SerialiserConfiguration.BitsPerComponent
                );
            }
            GUILayout.EndVertical();
        }
        
        private void LoggerGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout("Logger:", ref _showLoggerWindow);
            if (_showLoggerWindow)
            {
                _manager.LoggerConfiguration = (LoggerConfiguration)EditorGUILayout.ObjectField(
                    "Logger Configuration:",
                    _manager.LoggerConfiguration,
                    typeof(LoggerConfiguration),
                    false
                );
                if (_manager.LoggerConfiguration)
                    Editor.CreateEditor(_manager.LoggerConfiguration).OnInspectorGUI();
            }
            GUILayout.EndVertical();
        }
        
        #endregion
        
        #region managing
        
        private void ServerGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout("Server", ref _showServerWindow, _manager.IsServer, "Is Server:");
            if (_showServerWindow)
            {
                if (!_manager.IsServer)
                {
                    _servername = EditorGUILayout.TextField(new GUIContent("Servername:"), _servername);
                    if (GUILayout.Button(new GUIContent("Start Server")) && AllowStart())
                        _manager.StartServer(_servername);
                }
                else
                {
                    EditorGUILayout.TextField("Servername:", _manager.ServerInformation.Servername);
                    EditorGUILayout.TextField("Connected Clients:", $"{_manager.Server_ConnectedClients.Count}/{_manager.ServerInformation.MaxNumberConnectedClients}");
                    if (GUILayout.Button(new GUIContent("Stop Server")))
                        _manager.StopServer();
                
                    _serverClientsViewPos = EditorGUILayout.BeginScrollView(_serverClientsViewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150));
                    if (_manager.Server_ConnectedClients.Count == 0)
                    {
                        GUILayout.Label($"There are no clients connected to the local server!");
                    }
                    else
                    {
                        var defaultColour = _style.normal.textColor;
                        _style.alignment = TextAnchor.MiddleLeft;
                        for (var i = 0; i < _manager.Server_ConnectedClients.Count; i++)
                        {
                            var client = _manager.Server_ConnectedClients.Values.ElementAt(i);
                            EditorGUILayout.BeginHorizontal();
                            {
                                _style.normal.textColor = client.Colour;
                                GUILayout.Label($"#{client.ID} {client.Username}", _style);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        _style.normal.textColor = defaultColour;
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            GUILayout.EndVertical();
        }

        private void ClientGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToggleFoldout("Client", ref _showClientWindow, _manager.IsClient, "Is Client:");
            if (_showClientWindow)
            {
                if (!_manager.IsClient)
                {
                    _username = EditorGUILayout.TextField(new GUIContent("Username:"), _username);
                    _userColour = EditorGUILayout.ColorField(new GUIContent("User colour:"), _userColour);
                    if (GUILayout.Button(new GUIContent("Start Client")) && AllowStart())
                        _manager.StartClient(_username, _userColour);
                }
                else
                {
                    EditorGUILayout.TextField("ID:", $"{_manager.ClientInformation.ID}");
                    EditorGUILayout.TextField("Username:", _manager.ClientInformation.Username);
                    EditorGUILayout.ColorField("User colour:", _manager.ClientInformation.Colour);
                    EditorGUILayout.TextField("Servername:", _manager.ServerInformation.Servername);
                    EditorGUILayout.TextField("Connected Clients:", $"{_manager.Server_ConnectedClients.Count}/{_manager.ServerInformation.MaxNumberConnectedClients}");
                    if (GUILayout.Button(new GUIContent("Stop Client")))
                        _manager.StopClient();
                    
                    _clientClientsViewPos = EditorGUILayout.BeginScrollView(_clientClientsViewPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150));
                    if (_manager.Client_ConnectedClients.Count == 0)
                    {
                        GUILayout.Label($"There are no other clients connected to the server!");
                    }
                    else
                    {
                        var defaultColour = _style.normal.textColor;
                        _style.alignment = TextAnchor.MiddleLeft;
                        for (var i = 0; i < _manager.Client_ConnectedClients.Count; i++)
                        {
                            var client = _manager.Client_ConnectedClients.Values.ElementAt(i);
                            EditorGUILayout.BeginHorizontal();
                            {
                                _style.normal.textColor = client.Colour;
                                GUILayout.Label($"#{client.ID} {client.Username}", _style);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        _style.normal.textColor = defaultColour;
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            GUILayout.EndVertical();
        }
        
        #endregion
        
        #region utilities

        private bool AllowStart()
        {
            switch (_allowStart)
            {
                case EAllowStart.Anywhere:
                    return true;
                case EAllowStart.OnlyEditor:
                    return !EditorApplication.isPlaying;
                case EAllowStart.OnlyPlaymode:
                    return EditorApplication.isPlaying;
                default:
                    return false;
            }
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