using jKnepel.ProteusNet.Logging;
using System;
using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [CustomEditor(typeof(MonoNetworkManager))]
    internal class MonoNetworkManagerEditor : Editor
    {
        private MonoNetworkManager NetworkManager => (MonoNetworkManager)target;
        
        private Editor _networkObjectPrefabsEditor;
        
        private SerializedProperty _transportConfigProp;
        private SerializedProperty _serverListenAddressProp;
        private SerializedProperty _serverAddressProp;
        private SerializedProperty _portProp;
        private SerializedProperty _maxNumberOfClientsProp;

        private static readonly GUIContent LoggerConfigDesc = new("Logger");
        private static readonly GUIContent PrefabsConfigDesc = new("Network Prefabs");
        private static readonly GUIContent TickrateDesc = new("Tickrate");
        private static readonly GUIContent TransportConfigDesc = new("Transport Configuration", "Configuration settings for the transport layer.");
        private static readonly GUIContent ServerListenAddressDesc = new("Server Listen Address", "Address to which the local server will be bound. If no address is provided, the 0.0.0.0 address will be used instead.");
        private static readonly GUIContent ServerAddressDesc = new("Server Address", "The address to which the local client will attempt to connect with.");
        private static readonly GUIContent PortDesc = new("Port", "The port to which the local client will attempt to connect with or the server will bind to locally.");
        private static readonly GUIContent MaxNumberOfClientsDesc = new("Max Number of Clients", "The maximum number of connections allowed by the local server.");
        
        private void OnEnable()
        {
            _transportConfigProp = serializedObject.FindProperty("transportConfiguration");
            _serverListenAddressProp = serializedObject.FindProperty("<ServerListenAddress>k__BackingField");
            _serverAddressProp = serializedObject.FindProperty("<ServerAddress>k__BackingField");
            _portProp = serializedObject.FindProperty("<Port>k__BackingField");
            _maxNumberOfClientsProp = serializedObject.FindProperty("<MaxNumberOfClients>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            NetworkManager.LoggerConfiguration = (LoggerConfiguration)EditorGUILayout.ObjectField(LoggerConfigDesc, NetworkManager.LoggerConfiguration, typeof(LoggerConfiguration), false);
            NetworkManager.NetworkObjectPrefabs = (NetworkObjectPrefabs)EditorGUILayout.ObjectField(PrefabsConfigDesc, NetworkManager.NetworkObjectPrefabs, typeof(NetworkObjectPrefabs), false);
            NetworkManager.Tickrate = (uint)Math.Max(0, EditorGUILayout.IntField(TickrateDesc, (int)NetworkManager.Tickrate));
            EditorGUILayout.Space();
            
            GUILayout.Label("Transport", EditorStyles.boldLabel);
            
            var previousValue = _transportConfigProp.objectReferenceValue;
            EditorGUILayout.PropertyField(_transportConfigProp, TransportConfigDesc, true);
            if (previousValue == _transportConfigProp.objectReferenceValue || NetworkManager.IsOnline)
                _transportConfigProp.objectReferenceValue = previousValue;
            
            EditorGUILayout.PropertyField(_serverListenAddressProp, ServerListenAddressDesc);
            EditorGUILayout.PropertyField(_serverAddressProp, ServerAddressDesc);
            EditorGUILayout.PropertyField(_portProp, PortDesc);
            EditorGUILayout.PropertyField(_maxNumberOfClientsProp, MaxNumberOfClientsDesc);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
