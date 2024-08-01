using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Networking.Transporting
{
    [Serializable]
    public class TransportSettings
    {
        /// <summary>
        /// The type of protocol used by the unity transport.
        /// </summary>
        public EProtocolType ProtocolType = EProtocolType.UnityTransport;
        /// <summary>
        /// The address to which the local client will attempt to connect with.
        /// </summary>
        public string Address = "127.0.0.1";
        /// <summary>
        /// The port to which the local client will attempt to connect with or the server will bind to locally.
        /// </summary>
        public ushort Port = 24856;
        /// <summary>
        /// Address to which the local server will be bound. If no address is provided, the IPv4 Loopback
        /// address will be used instead.
        /// </summary>
        public string ServerListenAddress = string.Empty;
        /// <summary>
        /// The maximum number of connections allowed by the local server. 
        /// </summary>
        public uint MaxNumberOfClients = 100;
        /// <summary>
        /// Time between connection attempts.
        /// </summary>
        public uint ConnectTimeoutMS = 1000;
        /// <summary>
        /// Maximum number of connection attempts to try. If no answer is received from the server
        /// after this number of attempts, a disconnect event is generated for the connection.
        /// </summary>
        public uint MaxConnectAttempts = 60;
        /// <summary>
        /// Inactivity timeout for a connection. If nothing is received on a connection for this
        /// amount of time, it is disconnected. To prevent this from happening when the game session is simply
        /// quiet, set <c>HeartbeatTimeoutMS</c> to a positive non-zero value.
        /// </summary>
        public uint DisconnectTimeoutMS = 30000;
        /// <summary>
        /// Time after which if nothing from a peer is received, a heartbeat message will be sent
        /// to keep the connection alive. Prevents the <c>DisconnectTimeoutMS</c> mechanism from
        /// kicking when nothing happens on a connection. A value of 0 will disable heartbeats.
        /// </summary>
        public uint HeartbeatTimeoutMS = 500;
        /// <summary>
        /// Time after which to attempt to re-establish a connection if nothing is received from the
        /// peer. This is used to re-establish connections for example when a peer's IP address
        /// changes (e.g. mobile roaming scenarios). To be effective, should be less than
        /// <c>disconnectTimeoutMS</c> but greater than <c>heartbeatTimeoutMS</c>. A value of 0 will
        /// disable this functionality.
        /// </summary>
        public uint ReconnectionTimeoutMS = 2000;
        /// <summary>
        /// Maximum size that can be fragmented. Attempting to send a message larger than that will
        /// result in the send operation failing. Maximum value is ~20MB for unreliable packets,
        /// and ~88KB for reliable ones.
        /// </summary>
        public uint PayloadCapacity = 4096;
        /// <summary>
        /// Maximum number in-flight packets per pipeline/connection combination. Default value
        /// is 32 but can be increased to 64 at the cost of slightly larger packet headers.
        /// </summary>
        public uint WindowSize = 32;
        /// <summary>
        /// Minimum amount of time to wait before a reliable packet is resent if it's not been
        /// acknowledged.
        /// </summary>
        public uint MinimumResendTime = 64;
        /// <summary>
        /// Maximum amount of time to wait before a reliable packet is resent if it's not been
        /// acknowledged. That is, even with a high RTT the reliable pipeline will never wait
        /// longer than this value to resend a packet.
        /// </summary>
        public uint MaximumResendTime = 200;
        /// <summary>
        /// Whether the framework should automatically handle the tick rate on its own.
        /// If this value is set to false, the tick method must be called manually or no updates
        /// will be performed by the transport.
        /// </summary>
        public bool AutomaticTicks = true;
        /// <summary>
        /// The rate at which updates are performed per second. These updates include all network events,
        /// incoming and outgoing packets and client connections.
        /// </summary>
        public uint Tickrate = 30;
    }

    public enum EProtocolType
    {
        UnityTransport,
        UnityRelayTransport
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TransportSettings), true)]
    public class TransportSettingsDrawer : PropertyDrawer
    {
        private bool _areSettingsVisible;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _areSettingsVisible = EditorGUILayout.Foldout(_areSettingsVisible, "Settings", true);
            if (_areSettingsVisible)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ProtocolType"), new GUIContent("Protocol Type", "The type of protocol used by the protocol."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("Address"), new GUIContent("Address", "The address to which the local client will attempt to connect with."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("Port"), new GUIContent("Port", "The port to which the local client will attempt to connect with or the server will bind to locally."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ServerListenAddress"), new GUIContent("Server Listen Address", "Address to which the local server will be bound. If no address is provided, the IPv4 Loopback address will be used instead."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxNumberOfClients"), new GUIContent("Max Number of Clients", "The maximum number of connections allowed by the local server."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ConnectTimeoutMS"), new GUIContent("Connect Timeout", "Time between connection attempts."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxConnectAttempts"), new GUIContent("Max Connect Attempts", "Maximum number of connection attempts to try. If no answer is received from the server after this number of attempts, a disconnect event is generated for the connection."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("DisconnectTimeoutMS"), new GUIContent("Disconnect Timeout", "Inactivity timeout for a connection. If nothing is received on a connection for this amount of time, it is disconnected. To prevent this from happening when the game session is simply quiet, set HeartbeatTimeoutMS to a positive non-zero value."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("HeartbeatTimeoutMS"), new GUIContent("Heartbeat Timeout", "Time after which if nothing from a peer is received, a heartbeat message will be sent to keep the connection alive. Prevents the DisconnectTimeoutMS mechanism from kicking when nothing happens on a connection. A value of 0 will disable heartbeats."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ReconnectionTimeoutMS"), new GUIContent("Reconnection Timeout", "Time after which to attempt to re-establish a connection if nothing is received from the peer. This is used to re-establish connections for example when a peer's IP address changes (e.g. mobile roaming scenarios). To be effective, should be less than disconnectTimeoutMS but greater than heartbeatTimeoutMS. A value of 0 will disable this functionality."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("PayloadCapacity"), new GUIContent("Payload Capacity", "Maximum size that can be fragmented. Attempting to send a message larger than that will result in the send operation failing. Maximum value is ~20MB for unreliable packets, and ~88KB for reliable ones."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("WindowSize"), new GUIContent("Window Size", "Maximum number in-flight packets per pipeline/connection combination. Default value is 32 but can be increased to 64 at the cost of slightly larger packet headers."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MinimumResendTime"), new GUIContent("Minimum Resend Time", "Minimum amount of time to wait before a reliable packet is resent if it's not been acknowledged."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MaximumResendTime"), new GUIContent("Maximum Resend Time", "Maximum amount of time to wait before a reliable packet is resent if it's not been acknowledged. That is, even with a high RTT the reliable pipeline will never wait longer than this value to resend a packet."));

                var ticks = property.FindPropertyRelative("AutomaticTicks");
                EditorGUILayout.PropertyField(ticks, new GUIContent("Automatic Ticks", "Whether the framework should automatically handle the tick rate on its own. If this value is set to false, the tick method must be called manually or no updates will be performed by the transport."));
                if (ticks.boolValue)
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("Tickrate"), new GUIContent("Tickrate", "The rate at which updates are performed per second. These updates include all network events, incoming and outgoing packets and client connections."));
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
