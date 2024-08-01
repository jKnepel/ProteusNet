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
        public uint ProtocolID = 876237843;
        /// <summary>
        /// Multicast address on which an active local server will announce itself or where the server discovery will search. 
        /// </summary>
        public string DiscoveryIP = "239.240.240.149";
        /// <summary>
        /// Multicast port on which an active local server will announce itself or where the server discovery will search. 
        /// </summary>
        public ushort DiscoveryPort = 24857;
        /// <summary>
        /// The time after which discovered servers will be removed when no new announcement was received.
        /// </summary>
        public uint ServerDiscoveryTimeout = 3000;
        /// <summary>
        /// The interval in which an active local server will announce itself on the LAN.
        /// </summary>
        public uint ServerHeartbeatDelay = 500;
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ServerDiscoverySettings), true)]
    public class ServerDiscoverySettingsDrawer : PropertyDrawer
    {
        private bool _areSettingsVisible;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _areSettingsVisible = EditorGUILayout.Foldout(_areSettingsVisible, "Settings:", true);
            if (_areSettingsVisible)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ProtocolID"), new GUIContent("Protocol ID:", "Value used for identifying the protocol version of the server. Only servers with identical protocol IDs can be discovered."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("DiscoveryIP"), new GUIContent("Discovery IP:", "Multicast address on which an active local server will announce itself or where the server discovery will search."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("DiscoveryPort"), new GUIContent("Discovery Port:", "Multicast port on which an active local server will announce itself or where the server discovery will search."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ServerDiscoveryTimeout"), new GUIContent("Discovery Timeout:", "The time after which discovered servers will be removed when no new announcement was received."));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ServerHeartbeatDelay"), new GUIContent("Heartbeat Delay:", "The interval in which an active local server will announce itself on the LAN."));
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
