using jKnepel.ProteusNet.Managing;
using System;
using System.Collections.Generic;
using System.Globalization;
using jKnepel.ProteusNet.Networking.Transporting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.NetworkProfiler
{
    [Serializable]
    public class NetworkProfilerModule : Module
    {
        #region fields
        
        public override string Name => "NetworkProfiler";

        private NetworkProfilerSettings _settings;

        #endregion

        public NetworkProfilerModule(INetworkManager networkManager, ModuleConfiguration moduleConfig, NetworkProfilerSettings settings) 
            : base(networkManager, moduleConfig)
        {
            _settings = settings;

            if (!NetworkManager.IsInScope) return;
            
            GameObject singletonObject = new() { hideFlags = HideFlags.HideAndDontSave };
            var mono = singletonObject.AddComponent<ProfilerGUIMono>();
            mono.Initialize(NetworkManager, _settings);
        }
        
        private class ProfilerGUIMono : MonoBehaviour
        {
            private INetworkManager _manager;
            private NetworkProfilerSettings _settings;
            private readonly GUIStyle _style = new();

            private const float FPS_MEASURE_PERIOD = 0.5f;
            private float _frameTimeAccumulator;
            private float _averageFrameTime;
            private float _fpsNextPeriod;
            private int _fpsAccumulator;
            private int _currentFps;
            
            public NetworkMetrics TotalMetrics { get; private set; } = new();
            public List<NetworkMetrics> MetricsList { get; } = new();
            
            public void Initialize(INetworkManager manager, NetworkProfilerSettings settings)
            {
                _manager = manager;
                _settings = settings;
                _manager.OnTickCompleted += RetrieveMetrics;
            }
            
            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
                _fpsNextPeriod = Time.realtimeSinceStartup + FPS_MEASURE_PERIOD;
            }

            private void Update()
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

            private void OnGUI()
            {
                const float edge = 10f;
                const float width = 200f;
                var height = 70f; // 17.5f * 4 for four lines

                if (_settings.ShowNetworkManagerAPI)
                    height += 25f * 2; // 25f * 2 for two button lines
                
                float horizontal, vertical;
                switch (_settings.Alignment)
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

                if (_settings.ShowNetworkManagerAPI)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        using (new GUILayout.VerticalScope())
                        {
                            if (!_manager.IsServer && GUILayout.Button("Start Server"))
                                _manager.StartServer();
                            if (_manager.IsServer && GUILayout.Button("Stop Server"))
                                _manager.StopServer();
                            if (!_manager.IsClient && GUILayout.Button("Start Client"))
                                _manager.StartClient();
                            if (_manager.IsClient && GUILayout.Button("Stop Client"))
                                _manager.StopClient();
                        }
                        GUILayout.Space(5);
                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Toggle(_manager.IsServer, "Is Server");
                            GUILayout.Toggle(_manager.IsClient, "Is Client");
                        }
                    }
                }
                
                _style.normal.textColor = _settings.FontColor;

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
                    if (_manager is { IsClient: true })
                        rtt = _manager.Transport?.GetRTTToServer() ?? 0;
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
            
            private void RetrieveMetrics(uint _)
            {
                var metrics = _manager.Transport?.GetNetworkMetrics();
                Debug.Log(metrics?.PacketReceivedCount);
                if (metrics == null) return;
                
                MetricsList.Add(metrics);
                TotalMetrics.AddNetworkMetrics(metrics);
            }
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
        
#if UNITY_EDITOR
        private bool _areSettingsVisible;
        
        protected override void ModuleGUI()
        {
            EditorGUI.indentLevel++;
            _areSettingsVisible = EditorGUILayout.Foldout(_areSettingsVisible, "Settings", true);
            if (_areSettingsVisible)
            {
                _settings.Alignment = (ProfilerAlignment)EditorGUILayout.EnumPopup(new GUIContent("Alignment", "Where to align the profiler GUI in the visible window."), _settings.Alignment);
                _settings.ShowNetworkManagerAPI = EditorGUILayout.Toggle(new GUIContent("Show NetworkManager API", "Whether to show buttons for controlling the network manager in the profiler GUI."), _settings.ShowNetworkManagerAPI);
                _settings.FontColor = EditorGUILayout.ColorField(new GUIContent("Font Color", "The color used for the GUI font."), _settings.FontColor);
                _settings.ProfilerFilePath = EditorGUILayout.TextField(new GUIContent("Profiler Filepath", "The path to which network traffic statistics will be exported. If left empty, the default Unity persistent data path will be used."), _settings.ProfilerFilePath);
                _settings.ClientProfileFileName = EditorGUILayout.TextField(new GUIContent("Client Filename", "The filename used for the client traffic statistics."), _settings.ClientProfileFileName);
                _settings.ServerProfileFileName = EditorGUILayout.TextField(new GUIContent("Server Filename", "The filename used for the server traffic statistics."), _settings.ClientProfileFileName);
                EditorUtility.SetDirty(ModuleConfiguration);
            }
            
            EditorGUI.indentLevel--;
        }
#endif
    }
}
