using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Modules;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using jKnepel.ProteusNet.Serializing;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Components
{
	[AddComponentMenu("ProteusNet/Component/Network Manager (Mono)")]
    public class MonoNetworkManager : MonoBehaviour, INetworkManager
    {
	    [SerializeField] private TransportConfiguration _cachedTransportConfiguration;
	    public ATransport Transport => NetworkManager.Transport;
	    public TransportConfiguration TransportConfiguration
	    {
		    get => NetworkManager.TransportConfiguration;
		    set
		    {
			    if (NetworkManager.TransportConfiguration == value) return;
                NetworkManager.TransportConfiguration = _cachedTransportConfiguration = value;
			    
#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(_cachedTransportConfiguration);
			    if (!EditorApplication.isPlaying)
					EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }

	    [SerializeField] private SerializerConfiguration _cachedSerializerConfiguration;
	    public SerializerSettings SerializerSettings => NetworkManager.SerializerSettings;
	    public SerializerConfiguration SerializerConfiguration
	    {
		    get => NetworkManager.SerializerConfiguration;
		    set
		    {
			    if (NetworkManager.SerializerConfiguration == value) return;
			    NetworkManager.SerializerConfiguration = _cachedSerializerConfiguration = value;

#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(_cachedSerializerConfiguration);
                if (!EditorApplication.isPlaying)
				    EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }

	    [SerializeField] private LoggerConfiguration _cachedLoggerConfiguration;
	    public Logger Logger => NetworkManager.Logger;
	    public LoggerConfiguration LoggerConfiguration
	    {
		    get => NetworkManager.LoggerConfiguration;
		    set
		    {
			    if (NetworkManager.LoggerConfiguration == value) return;
			    NetworkManager.LoggerConfiguration = _cachedLoggerConfiguration = value;

#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(_cachedLoggerConfiguration);
                if (!EditorApplication.isPlaying)
				    EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }

	    [SerializeField] private List<ModuleConfiguration> _cachedModuleConfigs = new();
	    public ModuleList Modules => NetworkManager.Modules;

	    public Server Server => NetworkManager.Server;
	    public Client Client => NetworkManager.Client;
	    public Objects Objects => NetworkManager.Objects;

	    public bool IsServer => NetworkManager.IsServer;
	    public bool IsClient => NetworkManager.IsClient;
	    public bool IsOnline => NetworkManager.IsOnline;
	    public bool IsHost => NetworkManager.IsHost;

	    public EManagerScope ManagerScope => NetworkManager.ManagerScope;
	    public bool IsInScope => NetworkManager.IsInScope;

	    public bool UseAutomaticTicks => NetworkManager.UseAutomaticTicks;
	    public uint Tickrate => NetworkManager.Tickrate;
	    public uint CurrentTick => NetworkManager.CurrentTick;
	    
	    public event Action<uint> OnTickStarted
	    {
		    add => NetworkManager.OnTickStarted += value;
		    remove => NetworkManager.OnTickStarted -= value;
	    }
	    public event Action<uint> OnTickCompleted
	    {
		    add => NetworkManager.OnTickCompleted += value;
		    remove => NetworkManager.OnTickCompleted -= value;
	    }
	    public event Action OnTransportDisposed
	    {
		    add => NetworkManager.OnTransportDisposed += value;
		    remove => NetworkManager.OnTransportDisposed -= value;
	    }
	    public event Action<ServerReceivedData> OnServerReceivedData
	    {
		    add => NetworkManager.OnServerReceivedData += value;
		    remove => NetworkManager.OnServerReceivedData -= value;
	    }
	    public event Action<ClientReceivedData> OnClientReceivedData
	    {
		    add => NetworkManager.OnClientReceivedData += value;
		    remove => NetworkManager.OnClientReceivedData -= value;
	    }
	    public event Action<ELocalConnectionState> OnServerStateUpdated
	    {
		    add => NetworkManager.OnServerStateUpdated += value;
		    remove => NetworkManager.OnServerStateUpdated -= value;
	    }
	    public event Action<ELocalConnectionState> OnClientStateUpdated
	    {
		    add => NetworkManager.OnClientStateUpdated += value;
		    remove => NetworkManager.OnClientStateUpdated -= value;
	    }
	    public event Action<uint, ERemoteConnectionState> OnConnectionUpdated
	    {
		    add => NetworkManager.OnConnectionUpdated += value;
		    remove => NetworkManager.OnConnectionUpdated -= value;
	    }

	    private NetworkManager _networkManager;
	    /// <summary>
	    /// Instance of the internal network manager held by the scene context 
	    /// </summary>
	    public NetworkManager NetworkManager
	    {
		    get
		    {
			    if (_networkManager != null) return _networkManager;
			    _networkManager = new(EManagerScope.Runtime);
			    _networkManager.TransportConfiguration = _cachedTransportConfiguration;
			    _networkManager.SerializerConfiguration = _cachedSerializerConfiguration;
			    _networkManager.LoggerConfiguration = _cachedLoggerConfiguration;
			    
			    foreach (var config in _cachedModuleConfigs)
				    Modules.Add(config.GetModule(this));
			    
#if UNITY_EDITOR
			    NetworkManager.Modules.OnModuleAdded += OnModuleAdded;
			    NetworkManager.Modules.OnModuleRemoved += OnModuleRemoved;
			    NetworkManager.Modules.OnModuleInserted += OnModuleInserted;
			    NetworkManager.Modules.OnModuleRemovedAt += OnModuleRemovedAt;
#endif
			    
			    return _networkManager;
		    }
		    private set => _networkManager = value;
	    }

	    public void Tick() => NetworkManager.Tick();

	    public void StartServer() => NetworkManager.StartServer();
	    public void StopServer() => NetworkManager.StopServer();

	    public void StartClient() => NetworkManager.StartClient();
	    public void StopClient()=> NetworkManager.StopClient();

	    public void StartHost() => NetworkManager.StartHost();
	    public void StopHost() => NetworkManager.StopHost();
	    
	    #region private methods

	    private void Awake()
	    {	
		    // force getter initialisation if it has not been referenced yet
		    // network manager must be created in getter, because awake is not called during editor lifecycle
		    _ = NetworkManager;
	    }

	    private void OnDestroy()
	    {
		    NetworkManager.Dispose();
		    NetworkManager = null;
	    }

#if UNITY_EDITOR
	    private void OnModuleAdded(ModuleConfiguration config)
	    {
		    _cachedModuleConfigs.Add(config);
		    if (!EditorApplication.isPlaying)
			    EditorSceneManager.MarkSceneDirty(gameObject.scene);
	    }

	    private void OnModuleRemoved(ModuleConfiguration config)
	    {
		    _cachedModuleConfigs.Remove(config);
		    if (!EditorApplication.isPlaying)
			    EditorSceneManager.MarkSceneDirty(gameObject.scene);
	    }

	    private void OnModuleInserted(int index, ModuleConfiguration config)
	    {
		    _cachedModuleConfigs.Insert(index, config);
		    if (!EditorApplication.isPlaying)
			    EditorSceneManager.MarkSceneDirty(gameObject.scene);
	    }

	    private void OnModuleRemovedAt(int index)
	    {
		    _cachedModuleConfigs.RemoveAt(index);
		    if (!EditorApplication.isPlaying)
			    EditorSceneManager.MarkSceneDirty(gameObject.scene);
	    }
#endif
	    
	    #endregion
    }
}
