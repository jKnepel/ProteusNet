#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public enum EProtocolType
    {
        UnityTransport,
        UnityRelayTransport
    }

    public enum ESimulationState
    {
        Off,
        SendOnly,
        ReceiveOnly,
        Always
    }
    
    public partial class UnityTransport
    {
        /// <summary>
        /// The type of protocol used by the unity transport.
        /// </summary>
        public EProtocolType ProtocolType = EProtocolType.UnityTransport;
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
        public uint PayloadCapacity = 6144;
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
        /// Defines if and where the simulation settings are applied.
        /// Only updates on network restart.
        /// </summary>
        public ESimulationState NetworkSimulationState;
        /// <summary>
        /// The maximum amount of packets the pipeline can keep track of. This used when a
        /// packet is delayed, the packet is stored in the pipeline processing buffer and can
        /// be later brought back.
        /// </summary>
        public uint MaxPacketCount = 300;
        /// <summary>
        /// The maximum size of a packet which the simulator stores. If a packet exceeds this
        /// size it will bypass the simulator.
        /// </summary>
        public uint MaxPacketSize = 1400;
        /// <summary>
        /// Fixed delay in milliseconds to apply to all packets which pass through.
        /// </summary>
        public uint PacketDelayMs;
        /// <summary>
        /// Variance of the delay that gets added to all packets that pass through. For example,
        /// setting this value to 5 will result in the delay being a random value within 5
        /// milliseconds of the value set with <c>PacketDelayMs</c>.
        /// </summary>
        public uint PacketJitterMs;
        /// <summary>
        /// Fixed interval to drop packets on. This is most suitable for tests where predictable
        /// behaviour is desired, as every X-th packet will be dropped. For example, if the
        /// value is 5 every fifth packet is dropped.
        /// </summary>
        public uint PacketDropInterval;
        /// <summary>
        /// Percentage of packets that will be dropped.
        /// </summary>
        public float PacketDropPercentage;
        /// <summary>
        /// Percentage of packets that will be duplicated. Packets are duplicated at most once
        /// and will not be duplicated if they were first deemed to be dropped.
        /// </summary>
        public float PacketDuplicationPercentage;
        /// <summary>
        /// The fuzz factor is a percentage that represents both the proportion of packets that
        /// should be fuzzed, and the probability of any bit being flipped in the packet. For
        /// example, a value of 5 means about 5% of packets will be modified, and for each
        /// packet modified, each bit has a 5% chance of being flipped.
        /// </summary>
        public float FuzzFactor;
        /// <summary>
        /// To be used along the fuzz factor. The offset is the offset inside the packet where
        /// fuzzing should start. Useful to avoid fuzzing headers for example.
        /// </summary>
        public uint FuzzOffset;
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UnityTransport), true)]
    public class TransportSettingsDrawer : PropertyDrawer
    {
        private static readonly GUIContent ProtocolTypeLabel = new("Protocol Type", "The type of protocol used by the protocol.");
        private static readonly GUIContent ConnectTimeoutLabel = new("Connect Timeout", "Time between connection attempts.");
        private static readonly GUIContent MaxConnectAttemptsLabel = new("Max Connect Attempts", "Maximum number of connection attempts to try. If no answer is received from the server after this number of attempts, a disconnect event is generated for the connection.");
        private static readonly GUIContent DisconnectTimeoutLabel = new("Disconnect Timeout", "Inactivity timeout for a connection. If nothing is received on a connection for this amount of time, it is disconnected. To prevent this from happening when the game session is simply quiet, set HeartbeatTimeoutMS to a positive non-zero value.");
        private static readonly GUIContent HeartbeatTimeoutLabel = new("Heartbeat Timeout", "Time after which if nothing from a peer is received, a heartbeat message will be sent to keep the connection alive. Prevents the DisconnectTimeoutMS mechanism from kicking when nothing happens on a connection. A value of 0 will disable heartbeats.");
        private static readonly GUIContent ReconnectionTimeoutLabel = new("Reconnection Timeout", "Time after which to attempt to re-establish a connection if nothing is received from the peer. This is used to re-establish connections for example when a peer's IP address changes (e.g. mobile roaming scenarios). To be effective, should be less than disconnectTimeoutMS but greater than heartbeatTimeoutMS. A value of 0 will disable this functionality.");
        private static readonly GUIContent PayloadCapacityLabel = new("Payload Capacity", "Maximum size that can be fragmented. Attempting to send a message larger than that will result in the send operation failing. Maximum value is ~20MB for unreliable packets, and ~88KB for reliable ones.");
        private static readonly GUIContent WindowSizeLabel = new("Window Size", "Maximum number in-flight packets per pipeline/connection combination. Default value is 32 but can be increased to 64 at the cost of slightly larger packet headers.");
        private static readonly GUIContent MinimumResendTimeLabel = new("Minimum Resend Time", "Minimum amount of time to wait before a reliable packet is resent if it's not been acknowledged.");
        private static readonly GUIContent MaximumResendTimeLabel = new("Maximum Resend Time", "Maximum amount of time to wait before a reliable packet is resent if it's not been acknowledged. That is, even with a high RTT the reliable pipeline will never wait longer than this value to resend a packet.");

        private static readonly GUIContent NetworkSimulationStateLabel = new("Network Simulation State", "Defines if and where the simulation settings are applied. Only updates on network restart.");
        private static readonly GUIContent MaxPacketCountLabel = new("Max Packet Count", "The maximum amount of packets the pipeline can keep track of. This used when a packet is delayed, the packet is stored in the pipeline processing buffer and can be later brought back.");
        private static readonly GUIContent MaxPacketSizeLabel = new("Max Packet Size", "The maximum size of a packet which the simulator stores. If a packet exceeds this size it will bypass the simulator.");
        private static readonly GUIContent PacketDelayMsLabel = new("Packet Delay in Ms", "Fixed delay in milliseconds to apply to all packets which pass through.");
        private static readonly GUIContent PacketJitterMsLabel = new("Packet Jitter in Ms", "Variance of the delay that gets added to all packets that pass through. For example, setting this value to 5 will result in the delay being a random value within 5 milliseconds of the value set with PacketDelayMs.");
        private static readonly GUIContent PacketDropIntervalLabel = new("Packet Drop Interval", "Fixed interval to drop packets on. This is most suitable for tests where predictable behaviour is desired, as every X-th packet will be dropped. For example, if the value is 5 every fifth packet is dropped.");
        private static readonly GUIContent PacketDropPercentageLabel = new("Packet Drop Percentage", "Percentage of packets that will be dropped.");
        private static readonly GUIContent PacketDuplicationPercentageLabel = new("Packet Duplication Percentage");
        private static readonly GUIContent FuzzFactorLabel = new("Fuzz Factor", "Percentage of packets that will be duplicated. Packets are duplicated at most once and will not be duplicated if they were first deemed to be dropped. Value percentage of 0-100");
        private static readonly GUIContent FuzzOffsetLabel = new("Fuzz Offset", "To be used along the fuzz factor. The offset is the offset inside the packet where fuzzing should start. Useful to avoid fuzzing headers for example.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUILayout.PropertyField(property.FindPropertyRelative("ProtocolType"), ProtocolTypeLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ConnectTimeoutMS"), ConnectTimeoutLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxConnectAttempts"), MaxConnectAttemptsLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("DisconnectTimeoutMS"), DisconnectTimeoutLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("HeartbeatTimeoutMS"), HeartbeatTimeoutLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ReconnectionTimeoutMS"), ReconnectionTimeoutLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("PayloadCapacity"), PayloadCapacityLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("WindowSize"), WindowSizeLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MinimumResendTime"), MinimumResendTimeLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MaximumResendTime"), MaximumResendTimeLabel);

            EditorGUILayout.Space();

            var networkSimulationState = property.FindPropertyRelative("NetworkSimulationState");
            EditorGUILayout.PropertyField(networkSimulationState, NetworkSimulationStateLabel);
            if (networkSimulationState.boolValue)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxPacketCount"), MaxPacketCountLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxPacketSize"), MaxPacketSizeLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("PacketDelayMs"), PacketDelayMsLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("PacketJitterMs"), PacketJitterMsLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("PacketDropInterval"), PacketDropIntervalLabel);
                EditorGUILayout.Slider(property.FindPropertyRelative("PacketDropPercentage"), 0, 1, PacketDropPercentageLabel);
                EditorGUILayout.Slider(property.FindPropertyRelative("PacketDuplicationPercentage"), 0, 1, PacketDuplicationPercentageLabel);
                EditorGUILayout.Slider(property.FindPropertyRelative("FuzzFactor"), 0, 1, FuzzFactorLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("FuzzOffset"), FuzzOffsetLabel);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }

#endif
}
