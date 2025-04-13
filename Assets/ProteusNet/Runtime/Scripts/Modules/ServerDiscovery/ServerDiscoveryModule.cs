using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Utilities;
using jKnepel.ProteusNet.Serializing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    [Serializable]
    public class ServerDiscoveryModule : Module
    {
        #region fields
        
        public override string Name => "ServerDiscovery";
        
        [field: SerializeField] public ServerDiscoverySettings Settings { get; private set; } = new();

        private bool _isServerAnnounceActive;

        private bool _isServerDiscoveryActive;
        public bool IsServerDiscoveryActive
        {
            get => _isServerDiscoveryActive;
            private set
            {
                if (_isServerDiscoveryActive == value) return;

                _isServerDiscoveryActive = value;
                if (_isServerDiscoveryActive)
                    OnServerDiscoveryActivated?.Invoke();
                else
                    OnServerDiscoveryDeactivated?.Invoke();
            }
        }
        
        public List<DiscoveredServer> DiscoveredServers => _openServers.Values.ToList();

        public event Action OnServerDiscoveryActivated;
        public event Action OnServerDiscoveryDeactivated;
        public event Action OnActiveServerListUpdated;
        
        private byte[] _discoveryProtocolBytes;
		private IPAddress _discoveryIP;
        private UdpClient _discoveryClient;
        private Thread _discoveryThread;
        
        private byte[] _announceProtocolBytes;
		private IPAddress _announceIP;
        private UdpClient _announceClient;
        private Thread _announceThread;

        private readonly ConcurrentDictionary<IPEndPoint, DiscoveredServer> _openServers = new();
        private readonly SerializerSettings _serializerSettings = new() { UseCompression = false };

		#endregion

		#region public methods

        protected override void Initialize()
        {
            NetworkManager.Server.OnLocalStateUpdated += OnServerStateUpdated;
            if (NetworkManager.Server.LocalState == ELocalServerConnectionState.Started)
                StartServerAnnouncement();
            if (Settings.AutostartDiscovery)
                StartServerDiscovery();
        }

        protected override void Dispose(bool disposing)
        {
            EndServerDiscovery();
            EndServerAnnouncement();
        }

        #endregion
        
        #region server discovery

        public bool StartServerDiscovery()
        {
            if (!AllowStart())
                return false;
            if (IsServerDiscoveryActive)
                return true;

            try
            {
                _discoveryIP = IPAddress.Parse(Settings.DiscoveryIP);
                
                Writer writer = new(_serializerSettings);
                writer.WriteUInt32(Settings.ProtocolID);
                _discoveryProtocolBytes = writer.GetBuffer();

                _discoveryClient = new();
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_discoveryIP, IPAddress.Any));
                _discoveryClient.Client.Bind(new IPEndPoint(IPAddress.Any, Settings.DiscoveryPort));

                _discoveryThread = new(DiscoveryThread) { IsBackground = true };
                _discoveryThread.Start();

                return IsServerDiscoveryActive = true;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case FormatException:
                        Debug.LogError("The server discovery multicast IP is not a valid address!");
                        break;
                    case ObjectDisposedException:
                    case SocketException:
                        Debug.LogError("An error occurred when attempting to access the socket!");
                        break;
                    case ThreadStartException:
                        Debug.LogError("An error occurred when starting the threads. Please try again later!");
                        break;
                    case OutOfMemoryException:
                        Debug.LogError("Not enough memory available to start the threads!");
                        break;
                    default:
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                }
                
                return IsServerDiscoveryActive = false;
            }
        }

        public void EndServerDiscovery()
		{
            if (!IsServerDiscoveryActive)
                return;

            if (_discoveryClient != null)
            {
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(_discoveryIP, IPAddress.Any));
                _discoveryClient.Close();
                _discoveryClient.Dispose();
            }
            if (_discoveryThread != null)
            {
                _discoveryThread.Abort();
                _discoveryThread.Join();
            }

            _discoveryProtocolBytes = null;
            _discoveryIP = null;
            IsServerDiscoveryActive = false;
        }

        public bool RestartServerDiscovery()
		{
            EndServerDiscovery();
            return StartServerDiscovery(); 
        }
        
        private void DiscoveryThread()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEndpoint = new(0, 0);
                    var receivedBytes = _discoveryClient.Receive(ref remoteEndpoint);
                    Reader reader = new(receivedBytes, _serializerSettings);

                    // check crc32
                    var crc32 = reader.ReadUInt32();
                    var typePosition = reader.Position;
                    var bytesToHash = new byte[reader.Length];
                    var readerRemaining = reader.Remaining;
                    Buffer.BlockCopy(_discoveryProtocolBytes, 0, bytesToHash, 0, 4);
                    Buffer.BlockCopy(reader.ReadRemainingBuffer(), 0, bytesToHash, 4, readerRemaining);
                    if (crc32 != Hashing.GetCRC32Hash(bytesToHash))
                        continue;
                    
                    // read and update server
                    reader.Position = typePosition;
                    var packet = ServerAnnouncePacket.Read(reader);
                    IPEndPoint endpoint = new(remoteEndpoint.Address, packet.Port);
                    DiscoveredServer newServer = new(endpoint, packet.Servername, packet.MaxNumberOfClients, packet.NumberOfClients);
                    if (!_openServers.TryGetValue(endpoint, out _))
                        _ = TimeoutServer(endpoint);
                    _openServers[endpoint] = newServer;

                    MainThreadQueue.Enqueue(() => OnActiveServerListUpdated?.Invoke());
                }
                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case IndexOutOfRangeException:
                        case ArgumentException:
                            continue;
                        case SocketException:
                        case ThreadAbortException:
                            IsServerDiscoveryActive = false;
                            return;
                        default:
                            Debug.LogError("An error occurred in the server discovery!");
                            IsServerDiscoveryActive = false;
                            return;
                    }
                }
            }
        }

        private async Task TimeoutServer(IPEndPoint serverEndpoint)
        {
            await Task.Delay((int)Settings.ServerDiscoveryTimeout);
            if (_openServers.TryGetValue(serverEndpoint, out var server))
            {   // timeout and remove servers that haven't been updated for longer than the timeout value
                if ((DateTime.Now - server.LastHeartbeat).TotalMilliseconds > Settings.ServerDiscoveryTimeout)
                {
                    _openServers.TryRemove(serverEndpoint, out _);
                    MainThreadQueue.Enqueue(() => OnActiveServerListUpdated?.Invoke());
                    return;
                }

                _ = TimeoutServer(serverEndpoint);
            }
        }
        
        public void ConnectToDiscoveredServer(DiscoveredServer server)
        {
            NetworkManager.ServerAddress = server.Endpoint.Address.ToString();
            NetworkManager.Port = (ushort)server.Endpoint.Port;
            NetworkManager.StartClient();
        }
        
        #endregion
        
        #region server announce
        
        private void OnServerStateUpdated(ELocalServerConnectionState state)
        {
            switch (state)
            {
                case ELocalServerConnectionState.Started:
                    StartServerAnnouncement();
                    break;
                case ELocalServerConnectionState.Stopping:
                    EndServerAnnouncement();
                    break;
            }
        }
        
        private void StartServerAnnouncement()
        {
            try
            {
                _announceIP = IPAddress.Parse(Settings.DiscoveryIP);

                Writer writer = new(_serializerSettings);
                writer.WriteUInt32(Settings.ProtocolID);
                _announceProtocolBytes = writer.GetBuffer();
                
                _announceClient = new();
                _announceClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                _announceClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _announceClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                _announceClient.Client.Bind(new IPEndPoint(NetworkManager.Server.ServerEndpoint.Address, Settings.DiscoveryPort));
                _announceClient.Connect(new(_announceIP, Settings.DiscoveryPort));

                _announceThread = new(AnnounceThread) { IsBackground = true };
                _announceThread.Start();

                _isServerAnnounceActive = true;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case FormatException:
                        Debug.LogError("The server discovery multicast IP is not a valid address!");
                        break;
                    case ObjectDisposedException:
                    case SocketException:
                        Debug.LogError("An error occurred when attempting to access the socket!");
                        break;
                    case ThreadStartException:
                        Debug.LogError("An error occurred when starting the threads. Please try again later!");
                        break;
                    case OutOfMemoryException:
                        Debug.LogError("Not enough memory available to start the threads!");
                        break;
                    default:
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                }
            }
        }

        private void EndServerAnnouncement()
        {
            if (!_isServerAnnounceActive)
                return;
            
            if (_announceClient != null)
            {
                _announceClient.Close();
                _announceClient.Dispose();
            }
            if (_announceThread != null)
            {
                _announceThread.Abort();
                _announceThread.Join();
            }

            _announceProtocolBytes = null;
            _announceIP = null;
            _isServerAnnounceActive = false;
        }

        private void AnnounceThread()
        {
            while (true)
            {
                try
                {
                    // TODO : optimize this
                    Writer writer = new(_serializerSettings);
                    writer.Skip(4);
                    ServerAnnouncePacket.Write(writer, new(
                        (ushort)NetworkManager.Server.ServerEndpoint.Port,
                        NetworkManager.Server.Servername,
                        NetworkManager.Server.MaxNumberOfClients, 
                        (uint)NetworkManager.Server.ConnectedClients.Count
                    ));

                    var bytesToHash = new byte[writer.Length];
                    Buffer.BlockCopy(_announceProtocolBytes, 0, bytesToHash, 0, 4);
                    Buffer.BlockCopy(writer.GetBuffer(), 4, bytesToHash, 4, bytesToHash.Length - 4);
                    writer.Position = 0;
                    writer.WriteUInt32(Hashing.GetCRC32Hash(bytesToHash));

                    _announceClient.Send(writer.GetBuffer(), writer.Length);
                    Thread.Sleep((int)Settings.ServerHeartbeatDelay);
                }
                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case NullReferenceException:
                        case IndexOutOfRangeException:
                        case ArgumentException:
                            continue;
                        case ThreadAbortException:
                            return;
                        default:
                            ExceptionDispatchInfo.Capture(ex).Throw();
                            throw;
                    }
                }
            }
        }
        
        #endregion

        private bool AllowStart()
        {
            return NetworkManager.ManagerScope switch
            {
                EManagerScope.Runtime => Application.isPlaying,
                EManagerScope.Editor => !Application.isPlaying,
                _ => false
            };
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ServerDiscoveryModule), true)]
    public class ServerDiscoveryModuleDrawer : PropertyDrawer
    {
        private SavedBool _areSettingsVisible;
        
        private Texture2D _texture;
        private Texture2D Texture
        {
            get
            {
                if (_texture == null)
                    _texture = new(1, 1);
                return _texture;
            }
        }

        private Vector2 _scrollPos;
        private readonly Color[] _scrollViewColors = { new(0.25f, 0.25f, 0.25f), new(0.23f, 0.23f, 0.23f) };
        private const float ROW_HEIGHT = 20;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _areSettingsVisible ??= new($"{fieldInfo.GetType()}.ShowInfoFoldout", false);

            var target = (ServerDiscoveryModule)property.boxedValue;
            
            EditorGUI.BeginProperty(position, label, property);
            
            _areSettingsVisible.Value = EditorGUILayout.Foldout(_areSettingsVisible.Value, new GUIContent("Settings"), true);
            if (_areSettingsVisible)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<Settings>k__BackingField"));
                EditorGUI.indentLevel--;
            }
            
            using(new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
                if (!target.IsServerDiscoveryActive && GUILayout.Button("Start Server Discovery"))
                    target.StartServerDiscovery();
                if (target.IsServerDiscoveryActive && GUILayout.Button("Stop Server Discovery"))
                    target.EndServerDiscovery();
            }
            
            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
                GUILayout.Label("Discovered Servers", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Count: {target.DiscoveredServers?.Count}");
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.MinHeight(128.0f));
                using (new GUILayout.VerticalScope())
                {
                    for (var i = 0; i < target.DiscoveredServers?.Count; i++)
                    {
                        var server = target.DiscoveredServers[i];
                        EditorGUILayout.BeginHorizontal(GetScrollViewRowStyle(_scrollViewColors[i % 2]));
                        {
                            GUILayout.Label(server.Servername);
                            GUILayout.Label($"#{server.NumberConnectedClients}/{server.MaxNumberConnectedClients}");
                            if (GUILayout.Button(new GUIContent("Join Server"), GUILayout.ExpandWidth(false)))
                                target.ConnectToDiscoveredServer(server);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
        
        private GUIStyle GetScrollViewRowStyle(Color color)
        {
            Texture.SetPixel(0, 0, color);
            Texture.Apply();
            GUIStyle style = new()
            {
                normal = { background = Texture },
                fixedHeight = ROW_HEIGHT
            };
            return style;
        }
    }
#endif
}
