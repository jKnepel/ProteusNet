using jKnepel.ProteusNet.Managing;
using System;
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

            private const float FPSMeasurePeriod = 0.5f;
            private float _frameTimeAccumulator;
            private float _averageFrameTime;
            private float _fpsNextPeriod;
            private int _fpsAccumulator;
            private int _currentFps;
            
            public void Initialize(INetworkManager manager, NetworkProfilerSettings settings)
            {
                _manager = manager;
                _settings = settings;
            }
            
            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
                _fpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
            }

            private void Update()
            {
                _fpsAccumulator++;
                _frameTimeAccumulator += Time.deltaTime;
                if (Time.realtimeSinceStartup > _fpsNextPeriod)
                {
                    _currentFps = (int) (_fpsAccumulator / FPSMeasurePeriod);
                    _averageFrameTime = _frameTimeAccumulator / _fpsAccumulator * 1000f;
                    _fpsAccumulator = 0;
                    _frameTimeAccumulator = 0;
                    _fpsNextPeriod += FPSMeasurePeriod;
                }
            }

            private void OnGUI()
            {
                const float edge = 10f;
                const float width = 200f;
                var height = 52.5f; // 17.5f * 3 for three lines

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

                var stats = _manager.IsServer
                    ? _manager?.Logger.ServerTrafficStats 
                    : _manager.IsClient ? _manager?.Logger.ClientTrafficStats : null;
                    
                float inLast = 0f, inAvgBandwidth = 0f;
                float outLast = 0f, outAvgBandwidth = 0f;
                    
                if (stats is not null && stats.Count > 0)
                {
                    inLast = stats[^1].IncomingBytes;
                    outLast = stats[^1].OutgoingBytes;

                    ulong totalIn = 0ul, totalOut = 0ul;
                    foreach (var stat in stats)
                    {
                        totalIn += stat.IncomingBytes;
                        totalOut += stat.OutgoingBytes;
                    }
                        
                    inAvgBandwidth = totalIn * 8 / Time.realtimeSinceStartup;
                    outAvgBandwidth = totalOut * 8 / Time.realtimeSinceStartup;
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
                
                GUILayout.EndArea();
            }
        }

        public void ExportStatistics()
        {
            if (!NetworkManager.IsInScope) return;
            
            var filepath = _settings.ProfilerFilePath;
            if (string.IsNullOrEmpty(filepath))
                filepath = Application.persistentDataPath;
                
            if (NetworkManager.IsServer)
                NetworkManager?.Logger.ExportServerTrafficStats(filepath, _settings.ClientProfileFileName, false);
            else
                NetworkManager?.Logger.ExportClientTrafficStats(filepath, _settings.ServerProfileFileName, false);
        }
        
        private static string BandwidthToString(float bps)
        {
            switch (bps)
            {
                case >= 1_000_000f:
                {   // convert to Mbps
                    var mbps = bps / 1_000_000f;
                    return mbps.ToString("F2") + " Mbps";
                }
                case >= 1_000f:
                {   // convert to kbps
                    var kbps = bps / 1_000f;
                    return kbps.ToString("F2") + " kbps";
                }
                default:
                {   // keep in bps
                    return bps.ToString("F2") + " bps";
                }
            }
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

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 10);
                if (GUILayout.Button("Export Statistics"))
                    ExportStatistics();
            }
            
            EditorGUI.indentLevel--;
        }
#endif
    }
}
