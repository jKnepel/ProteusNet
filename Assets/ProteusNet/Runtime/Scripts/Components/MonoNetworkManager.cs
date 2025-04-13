using jKnepel.ProteusNet.Components.Configuration;
using jKnepel.ProteusNet.Logging;
using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Logger = jKnepel.ProteusNet.Logging.Logger;

namespace jKnepel.ProteusNet.Components
{
	[DefaultExecutionOrder(-1000)]
	[AddComponentMenu("ProteusNet/Component/Network Manager (Mono)")]
    public class MonoNetworkManager : MonoBehaviour, INetworkManager
    {
	    #region configurations
	    
	    public Logger Logger { get; private set; }
	    [SerializeField] private LoggerConfiguration loggerConfiguration;
	    public LoggerConfiguration LoggerConfiguration
	    {
		    get => loggerConfiguration;
		    set
		    {
			    if (loggerConfiguration == value) return;
			    if (IsOnline)
			    {
				    Debug.LogError("Can't change the configuration while a local connection is established!");
				    return;
			    }
			    
			    loggerConfiguration = value;
			    if (loggerConfiguration is not null && IsInScope)
				    Logger = loggerConfiguration.Value;

#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(LoggerConfiguration);
                if (!EditorApplication.isPlaying)
				    EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }
	    
	    [SerializeField] private NetworkObjectPrefabs networkObjectPrefabs;
	    public NetworkObjectPrefabs NetworkObjectPrefabs
	    {
		    get => networkObjectPrefabs;
		    set
		    {
			    if (networkObjectPrefabs == value) return;
			    if (IsOnline)
			    {
				    Debug.LogError("Can't change the configuration while a local connection is established!");
				    return;
			    }
			    
			    networkObjectPrefabs = value;
			    
#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(networkObjectPrefabs);
			    if (!EditorApplication.isPlaying)
				    EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }
	    
	    [SerializeField] private uint tickrate = 30;
	    public uint Tickrate
	    {
		    get => tickrate;
		    set
		    {
			    if (value == tickrate) return;
			    if (IsOnline)
			    {
				    Debug.LogError("Can't change the tickrate while a local connection is established!");
				    return;
			    }

			    tickrate = value;
		    }
	    }
	    
	    public uint CurrentTick { get; private set; }
	    
	    [SerializeField] private AConfigurationComponent<ATransport> transportConfiguration;
	    public AConfigurationComponent<ATransport> TransportConfiguration
	    {
		    get => transportConfiguration;
		    set
		    {
			    if (transportConfiguration == value) return;
			    if (IsOnline)
			    {
				    Debug.LogError("Can't change the configuration while a local connection is established!");
				    return;
			    }
			    
			    transportConfiguration = value;
			    Transport = transportConfiguration == null ? null : transportConfiguration.Value;
			    
#if UNITY_EDITOR
			    if (value != null)
				    EditorUtility.SetDirty(transportConfiguration);
			    if (!EditorApplication.isPlaying)
				    EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
		    }
	    }
	    
	    private ATransport _transport;
	    public ATransport Transport
	    {
		    get => _transport;
		    private set
		    {
			    if (value == _transport) return;
			    _transport = value;
			    OnTransportExchanged?.Invoke();
		    }
	    }
	    
	    [field: SerializeField] public string ServerListenAddress { get; set; } = string.Empty;

	    [field: SerializeField] public string ServerAddress { get; set; } = "127.0.0.1";

	    [field: SerializeField] public ushort Port { get; set; } = 24856;
	    
	    [field: SerializeField] public uint MaxNumberOfClients { get; set; } = 100;

	    public Server Server { get; private set; }
	    public Client Client { get; private set; }
	    public Objects Objects { get; private set; }

	    public bool IsServer => Server is { IsActive: true };
	    public bool IsClient => Client is { IsActive: true };
	    public bool IsOnline => IsServer || IsClient;
	    public bool IsHost => IsServer && IsClient;

	    public EManagerScope ManagerScope => EManagerScope.Runtime;
	    public bool IsInScope => Application.isPlaying;

	    public event Action<uint> OnTickStarted;
	    public event Action<uint> OnTickCompleted;
	    public event Action OnTransportExchanged;
	    
	    private bool _disposed;
	    private bool _ticksStarted;
	    private float _tickInterval;
	    private float _elapsedInterval;
	    
	    #endregion
	    
	    #region public methods

	    public void StartServer()
	    {
		    if (!IsInScope) return;

		    if (Transport == null && TransportConfiguration != null)
			    Transport = TransportConfiguration.Value;
		    if (Transport == null)
		    {
			    Debug.LogError("The transport must be defined before a server can be started!");
			    return;
		    }

		    Logger?.Reset();

		    Transport?.StartServer(ServerListenAddress, Port, MaxNumberOfClients);
		    StartTicks();
	    }

	    public void StopServer()
	    {
		    if (!IsInScope) return;
            
		    Transport?.StopServer();
		    if (!IsOnline)
			    StopTicks();
	    }

	    public void StartClient()
	    {
		    if (!IsInScope) return;
            
		    if (Transport == null && TransportConfiguration != null)
			    Transport = TransportConfiguration.Value;
		    if (Transport == null)
		    {
			    Debug.LogError("The transport must be defined before a client can be started!");
			    return;
		    }
            
		    if (!IsOnline)
			    Logger?.Reset();

		    Transport?.StartClient(ServerAddress, Port);
		    StartTicks();
	    }
        
	    public void StopClient()
	    {
		    if (!IsInScope) return;
            
		    Transport?.StopClient();
		    if (!IsOnline)
			    StopTicks();
	    }

	    public void StartHost()
	    {
		    StartServer();
		    StartClient();
	    }

	    public void StopHost()
	    {
		    StopClient();
		    StopServer();
	    }
	    
	    #endregion
	    
	    #region private methods

	    private void Awake()
	    {
		    Logger = LoggerConfiguration ? LoggerConfiguration.Value : null;
		    Objects = new();
		    Server = GetOrCreateConfiguration<Server>();
		    Client = GetOrCreateConfiguration<Client>();
		    
		    Server.Initialize(this);
		    Client.Initialize(this);
	    }

	    private void Update()
	    {
		    if (!_ticksStarted)
			    return;
            
		    _elapsedInterval += Time.deltaTime;
		    if (_elapsedInterval < _tickInterval) return;
            
		    OnTickStarted?.Invoke(CurrentTick);
		    Transport?.Tick();
		    OnTickCompleted?.Invoke(CurrentTick);
		    CurrentTick++;
		    _elapsedInterval = 0;
	    }

	    private T GetOrCreateConfiguration<T>() where T : class
	    {
		    var configuration = gameObject.GetComponent<AConfigurationComponent<T>>();
		    if (configuration) return configuration.Value;
		    
		    configuration = typeof(T) switch
		    {
			    var t when t == typeof(Server) => gameObject.AddComponent<ServerConfiguration>() as AConfigurationComponent<T>,
			    var t when t == typeof(Client) => gameObject.AddComponent<ClientConfiguration>() as AConfigurationComponent<T>,
			    _ => null
		    };

		    return configuration ? configuration.Value : null;
	    }

	    private void StartTicks()
	    {
		    CurrentTick = 0;
		    _ticksStarted = true;
		    _tickInterval = 1f / Tickrate;
	    }

	    private void StopTicks()
	    {
		    CurrentTick = 0;
		    _ticksStarted = false;
		    _tickInterval = 0;
	    }
	    
	    #endregion
    }
}
