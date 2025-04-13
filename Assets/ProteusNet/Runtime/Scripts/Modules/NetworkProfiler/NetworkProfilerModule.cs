using jKnepel.ProteusNet.Networking.Transporting;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.NetworkProfiler
{
    [Serializable]
    public class NetworkProfilerModule : Module
    {
        public override string Name => "NetworkProfiler";

        [field: SerializeField] public NetworkProfilerSettings Settings { get; private set; } = new();
        public NetworkMetrics TotalMetrics { get; } = new();
        public List<NetworkMetrics> MetricsList { get; } = new();
        
        private readonly GUIStyle _style = new();

        private const float FPS_MEASURE_PERIOD = 0.5f;
        private float _frameTimeAccumulator;
        private float _averageFrameTime;
        private float _fpsNextPeriod;
        private int _fpsAccumulator;
        private int _currentFps;

        public void Update()
        {
            _fpsAccumulator++;
            _frameTimeAccumulator += Time.deltaTime;
            if (Time.realtimeSinceStartup > _fpsNextPeriod)
            {
                _currentFps = (int) (_fpsAccumulator / FPS_MEASURE_PERIOD);
                _averageFrameTime = _frameTimeAccumulator / _fpsAccumulator * 1000f;
                _fpsAccumulator = 0;
                _frameTimeAccumulator = 0;
                _fpsNextPeriod += FPS_MEASURE_PERIOD;
            }
        }

        public void GUI()
        {
            const float edge = 10f;
            const float width = 200f;
            var height = 70f; // 17.5f * 4 for four lines

            if (Settings.ShowNetworkManagerAPI)
                height += 25f * 2; // 25f * 2 for two button lines
            
            float horizontal, vertical;
            switch (Settings.Alignment)
            {
                case ProfilerAlignment.TopLeft:
                    horizontal = edge;
                    vertical = edge;
                    break;
                case ProfilerAlignment.TopRight:
                    horizontal = Screen.width - width - edge;
                    vertical = edge;
                    break;
                case ProfilerAlignment.BottomLeft:
                    horizontal = edge;
                    vertical = Screen.height - height - edge;
                    break;
                case ProfilerAlignment.BottomRight:
                    horizontal = Screen.width - width - edge;
                    vertical = Screen.height - height - edge;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUILayout.BeginArea(new(horizontal, vertical, width, height), new GUIStyle("Box"));

            if (Settings.ShowNetworkManagerAPI)
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        if (!NetworkManager.IsServer && GUILayout.Button("Start Server"))
                            NetworkManager.StartServer();
                        if (NetworkManager.IsServer && GUILayout.Button("Stop Server"))
                            NetworkManager.StopServer();
                        if (!NetworkManager.IsClient && GUILayout.Button("Start Client"))
                            NetworkManager.StartClient();
                        if (NetworkManager.IsClient && GUILayout.Button("Stop Client"))
                            NetworkManager.StopClient();
                    }
                    GUILayout.Space(5);
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Toggle(NetworkManager.IsServer, "Is Server");
                        GUILayout.Toggle(NetworkManager.IsClient, "Is Client");
                    }
                }
            }
            
            _style.normal.textColor = Settings.FontColor;

            float inLast = 0f, inAvgBandwidth = 0f;
            float outLast = 0f, outAvgBandwidth = 0f;
                
            if (MetricsList is { Count: > 0 })
            {
                inLast = MetricsList[^1].PacketReceivedSize;
                outLast = MetricsList[^1].PacketSentSize;
            }

            if (TotalMetrics != null)
            {
                inAvgBandwidth = TotalMetrics.PacketReceivedSize * 8 / Time.realtimeSinceStartup;
                outAvgBandwidth = TotalMetrics.PacketSentSize * 8 / Time.realtimeSinceStartup;
            }
            
            using (new GUILayout.HorizontalScope())
            {
                var rtt = 0;
                if (NetworkManager is { IsClient: true })
                    rtt = NetworkManager.Transport?.GetRTTToServer() ?? 0;
                GUILayout.Label($"RTT: {rtt}", _style);
                GUILayout.Space(2);
                GUILayout.Label($"FPS: {_currentFps.ToString().PadLeft(4)[..4]} ({_averageFrameTime:F1}ms)", _style);
            }
                
            GUILayout.Label($"in: {inLast.ToString(CultureInfo.CurrentCulture),4} {BandwidthToString(inAvgBandwidth)}", _style);
            GUILayout.Label($"out: {outLast.ToString(CultureInfo.CurrentCulture),4} {BandwidthToString(outAvgBandwidth)}", _style);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"dropped: {NumberToString(TotalMetrics?.PacketsDropped ?? 0)}", _style);
                GUILayout.Space(2);
                GUILayout.Label($"resent: {NumberToString(TotalMetrics?.PacketsResent ?? 0)}", _style);
            }
            
            GUILayout.EndArea();
        }
        
        public void OnTickComplete(uint _)
        {
            var metrics = NetworkManager.Transport?.GetNetworkMetrics();
            if (metrics == null) return;
                
            MetricsList.Add(metrics);
            TotalMetrics.AddNetworkMetrics(metrics);
        }
        
        private static string BandwidthToString(float bps)
        {
            switch (bps)
            {
                case >= 1_000_000f:
                {   // convert to Mbps
                    var mbps = bps / 1_000_000f;
                    return mbps.ToString("F2") + "Mbps";
                }
                case >= 1_000f:
                {   // convert to kbps
                    var kbps = bps / 1_000f;
                    return kbps.ToString("F2") + "kbps";
                }
                default:
                {   // keep in bps
                    return bps.ToString("F2") + "bps";
                }
            }
        }
        
        private static string NumberToString(uint number)
        {
            if (number < 1000)
                return number.ToString();

            string[] suffixes = { "k", "M", "B", "T" };
            var suffixIndex = -1;
            double simplifiedNumber = number;

            while (simplifiedNumber >= 1000 && suffixIndex < suffixes.Length - 1)
            {
                simplifiedNumber /= 1000;
                suffixIndex++;
            }

            return $"{simplifiedNumber:0.#}{suffixes[suffixIndex]}";
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(NetworkProfilerModule), true)]
    public class NetworkProfilerModuleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("<Settings>k__BackingField"));
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
