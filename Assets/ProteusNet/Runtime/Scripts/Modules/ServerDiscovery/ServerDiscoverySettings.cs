using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    [Serializable]
    public class ServerDiscoverySettings
    {
        /// <summary>
        /// Value used for identifying the protocol version of the server. Only servers with identical protocol IDs can be discovered.
        /// </summary>
        [field: SerializeField] public uint ProtocolID { get; set; } = 876237843;
        /// <summary>
        /// Multicast address on which an active local server will announce itself or where the server discovery will search. 
        /// </summary>
        [field: SerializeField] public string DiscoveryIP { get; set; } = "239.240.240.149";
        /// <summary>
        /// Multicast port on which an active local server will announce itself or where the server discovery will search. 
        /// </summary>
        [field: SerializeField] public ushort DiscoveryPort { get; set; } = 24857;
        /// <summary>
        /// The time after which discovered servers will be removed when no new announcement was received.
        /// </summary>
        [field: SerializeField] public uint ServerDiscoveryTimeout { get; set; } = 3000;
        /// <summary>
        /// The interval in which an active local server will announce itself on the LAN.
        /// </summary>
        [field: SerializeField] public uint ServerHeartbeatDelay { get; set; } = 500;
        /// <summary>
        /// Whether to autostart the discovery in the editor or runtime.
        /// </summary>
        [field: SerializeField]  public bool AutostartDiscovery { get; set; } = false;
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ServerDiscoverySettings), true)]
    public class ServerDiscoverySettingsDrawer : PropertyDrawer
    {
        private static readonly GUIContent ProtocolIDDesc = new("Protocol ID", "Value used for identifying the protocol version of the server. Only servers with identical protocol IDs can be discovered.");
        private static readonly GUIContent DiscoveryIPDesc = new("Discovery IP", "Multicast address on which an active local server will announce itself or where the server discovery will search.");
        private static readonly GUIContent DiscoveryPortDesc = new("Discovery Port", "Multicast port on which an active local server will announce itself or where the server discovery will search.");
        private static readonly GUIContent ServerDiscoveryTimeoutDesc = new("Discovery Timeout", "The time after which discovered servers will be removed when no new announcement was received.");
        private static readonly GUIContent AServerHeartbeatDelayDesc = new("Heartbeat Delay", "The interval in which an active local server will announce itself on the LAN.");
        private static readonly GUIContent AutostartDiscoveryDesc = new("Autostart Discovery", "Whether to autostart the discovery in the editor or runtime.");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<ProtocolID>k__BackingField"), ProtocolIDDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<DiscoveryIP>k__BackingField"), DiscoveryIPDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<DiscoveryPort>k__BackingField"), DiscoveryPortDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<ServerDiscoveryTimeout>k__BackingField"), ServerDiscoveryTimeoutDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<ServerHeartbeatDelay>k__BackingField"), AServerHeartbeatDelayDesc);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<AutostartDiscovery>k__BackingField"), AutostartDiscoveryDesc);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
