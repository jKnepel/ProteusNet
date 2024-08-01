using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serialising;
using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet.Managing
{
    [CustomEditor(typeof(MonoNetworkManager))]
    internal class MonoNetworkManagerEditor : Editor
    {
        private MonoNetworkManager NetworkManager => (MonoNetworkManager)target;

        private INetworkManagerEditor _networkManagerEditor;
        private INetworkManagerEditor NetworkManagerEditor
        {
            get
            {
                if (_networkManagerEditor != null)
                    return _networkManagerEditor;
                return _networkManagerEditor = new(NetworkManager, INetworkManagerEditor.EAllowStart.OnlyPlaymode);
            }
        }
        
        [SerializeField] private bool _showTransportWindow = true;
        [SerializeField] private bool _showSerialiserWindow = true;
        [SerializeField] private bool _showLoggerWindow = true;


        private void Awake()
        {
            NetworkManager.Client.OnLocalStateUpdated += RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientConnected += RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientDisconnected += RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientUpdated += RepaintOnUpdate;
            NetworkManager.Client.OnServerUpdated += RepaintOnUpdate;
            NetworkManager.Server.OnLocalStateUpdated += RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientConnected += RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientDisconnected += RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientUpdated += RepaintOnUpdate;
            NetworkManager.Server.OnServerUpdated += RepaintOnUpdate;
        }

        private void OnDestroy()
        {
            NetworkManager.Client.OnLocalStateUpdated -= RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientConnected -= RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientDisconnected -= RepaintOnUpdate;
            NetworkManager.Client.OnRemoteClientUpdated -= RepaintOnUpdate;
            NetworkManager.Client.OnServerUpdated -= RepaintOnUpdate;
            NetworkManager.Server.OnLocalStateUpdated -= RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientConnected -= RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientDisconnected -= RepaintOnUpdate;
            NetworkManager.Server.OnRemoteClientUpdated -= RepaintOnUpdate;
            NetworkManager.Server.OnServerUpdated -= RepaintOnUpdate;
        }

        private void RepaintOnUpdate() => Repaint();
        private void RepaintOnUpdate(uint _) => Repaint();
        private void RepaintOnUpdate(ELocalClientConnectionState _) => Repaint();
        private void RepaintOnUpdate(ELocalServerConnectionState _) => Repaint();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Configurations:", EditorStyles.boldLabel);
            TransportGUI();
            SerialiserGUI();
            LoggerGUI();
            
            EditorGUILayout.Space();
            GUILayout.Label("Modules:", EditorStyles.boldLabel);
            NetworkManagerEditor.ModuleGUI();

            EditorGUILayout.Space();
            GUILayout.Label("Managers:", EditorStyles.boldLabel);
            NetworkManagerEditor.ServerGUI();
            NetworkManagerEditor.ClientGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private void TransportGUI()
        {
            NetworkManager.TransportConfiguration = NetworkManagerEditor.ConfigurationGUI<TransportConfiguration>(NetworkManager.TransportConfiguration, "Transport", ref _showTransportWindow);
        }

        private void SerialiserGUI()
        {
            NetworkManager.SerialiserConfiguration = NetworkManagerEditor.ConfigurationGUI<SerialiserConfiguration>(NetworkManager.SerialiserConfiguration, "Serialiser", ref _showSerialiserWindow);
        }

        private void LoggerGUI()
        {
            NetworkManager.LoggerConfiguration = NetworkManagerEditor.ConfigurationGUI<LoggerConfiguration>(NetworkManager.LoggerConfiguration, "Logger", ref _showLoggerWindow);
        }
    }
}
