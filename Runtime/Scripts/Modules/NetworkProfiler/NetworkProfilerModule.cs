using jKnepel.ProteusNet.Managing;
using System;
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
            
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return;
#endif
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
                    _averageFrameTime = (_frameTimeAccumulator / _fpsAccumulator) * 1000f;
                    _fpsAccumulator = 0;
                    _frameTimeAccumulator = 0;
                    _fpsNextPeriod += FPSMeasurePeriod;
                }
            }

            private void OnGUI()
            {
                const float width = 200f;
                const float height = 200f;
                const float edge = 10f;
                float horizontal, vertical;
                
                switch (_settings.Alignment)
                {
                    case ProfilerAlignment.TopLeft:
                        horizontal = 10f;
                        vertical = 10f;
                        break;
                    case ProfilerAlignment.TopRight:
                        horizontal = Screen.width - width - edge;
                        vertical = 10f;
                        break;
                    case ProfilerAlignment.BottomLeft:
                        horizontal = 10f;
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
                
                using (new GUILayout.HorizontalScope())
                {   // system metrics   
                    _style.normal.textColor = _settings.FontColor;
                    var rtt = _manager.Transport?.GetRTTToServer();
                    
                    GUILayout.Label($"RTT: {rtt}", _style);
                    GUILayout.Space(2);
                    GUILayout.Label($"FPS: {_currentFps.ToString().PadLeft(4)[..4]} ({_averageFrameTime:F1}ms)", _style);
                }

                if (_manager.IsOnline)
                {
                    var stats = _manager.IsServer
                        ? _manager?.Logger.ServerTrafficStats 
                        : _manager?.Logger.ClientTrafficStats;
                    
                    float inLast = 0f, inAvgBandwidth = 0f, inAvgPackets = 0f;
                    float outLast = 0f, outAvgBandwidth = 0f, outAvgPackets = 0f;
                    
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
                    
                    GUILayout.Label($"in:  {inLast.ToString(),4} {BandwidthToString(inAvgBandwidth)} {inAvgPackets}/s", _style);
                    GUILayout.Label($"out: {outLast.ToString(),4} {BandwidthToString(outAvgBandwidth)} {outAvgPackets}/s", _style);
                }
                
                GUILayout.EndArea();
            }
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
                _settings.Alignment = (ProfilerAlignment)EditorGUILayout.EnumPopup(new GUIContent("Alignment"), _settings.Alignment);
                _settings.ShowNetworkManagerAPI = EditorGUILayout.Toggle(new GUIContent("Show NetworkManager API"), _settings.ShowNetworkManagerAPI);
                _settings.FontColor = EditorGUILayout.ColorField(new GUIContent("Font Color"), _settings.FontColor);
                EditorUtility.SetDirty(ModuleConfiguration);
            }
            EditorGUI.indentLevel--;
        }
#endif
    }
}