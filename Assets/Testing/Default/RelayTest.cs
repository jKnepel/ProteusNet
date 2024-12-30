using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Transporting;
using System;
using System.Linq;
using System.Threading.Tasks;
using jKnepel.ProteusNet.Components;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RelayTest : MonoBehaviour
{
    [SerializeField] private MonoNetworkManager _manager;
    [SerializeField] private uint _targetClientID;
    [SerializeField] private string _message;
    
    public bool IsOnline => _manager.IsOnline;
    public bool IsServer => _manager.IsServer;
    public bool IsClient => _manager.IsClient;
    public bool IsHost => _manager.IsHost;
    public string PlayerID { get; private set; }
    public string[] AllocationRegions { get; private set; }
    public string JoinCode { get; private set; }

    private void Start()
    {
        _ = InitializeNetwork();
    }
    
    public async Task InitializeNetwork()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("An Error occurred trying to sign in the Player. Try again Later!");
            return;
        }

        PlayerID = AuthenticationService.Instance.PlayerId;
        AllocationRegions = (await RelayService.Instance.ListRegionsAsync()).Select(x => x.Id).ToArray();

        Debug.Log("The Player was Signed in!");
    }

    public async void StartServer(int maxPlayers, string allocationRegion)
    {
        if (_manager.Transport == null) return;
        
        var hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers, allocationRegion);
        JoinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
        ((UnityTransport)_manager.Transport).SetRelayServerData(new(hostAllocation, "dtls"));
        _manager.StartServer();
    }

    public void StopServer()
    {
        _manager.StopServer();
    }

    public async void StartClient(string joinCode)
    {
        if (_manager.Transport == null) return;
        
        if (IsServer)
        {
            _manager.StartClient();
            return;
        }
        
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        ((UnityTransport)_manager.Transport).SetRelayServerData(new(joinAllocation, "udp"));
        _manager.StartClient();
    }

    public void StopClient()
    {
        _manager.StopClient();
    }

    public void Register()
    {
        _manager.Client.RegisterStructData<MessageStruct>(ReceiveStruct);
    }

    public void Unregister()
    {
        _manager.Client.UnregisterStructData<MessageStruct>(ReceiveStruct);
    }

    public void SendToClient(ENetworkChannel channel = ENetworkChannel.ReliableOrdered)
    {
        MessageStruct message = new()
        {
            String = _message,
            Byte = 1,
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123,
            Ints = new [] { 1, 2, 3 }
        };
        _manager.Client.SendStructDataToClient(_targetClientID, message, channel);
    }

    private void ReceiveStruct(StructData<MessageStruct> data)
    {
        Debug.Log($"Received from {data.SenderID} during tick {data.Tick} at {data.Timestamp}:\n" +
                  $"String = {data.Data.String},\n" +
                  $"Byte = {data.Data.Byte},\n" +
                  $"Short = {data.Data.Short},\n" +
                  $"UShort = {data.Data.UShort},\n" +
                  $"Int = {data.Data.Int},\n" +
                  $"UInt = {data.Data.UInt},\n" +
                  $"Long = {data.Data.Long},\n" +
                  $"ULong = {data.Data.ULong}\n" +
                  $"Ints = {string.Join(",", data.Data.Ints)}\n");
    }

    private struct MessageStruct
    {
        public string String;
        public byte Byte;
        public short Short;
        public ushort UShort;
        public int Int;
        public uint UInt;
        public long Long;
        public ulong ULong;
        public int[] Ints;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RelayTest))]
public class RelayTestEditor : Editor
{
    private ENetworkChannel _channel = ENetworkChannel.ReliableOrdered;
    private int _maxNumberPlayers = 5;
    private int _allocationRegionIdx = 0;
    private string _joinCode = string.Empty;
    
	public override void OnInspectorGUI()
	{
        var test = (RelayTest)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_manager"));
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Values:", EditorStyles.boldLabel);
        GUILayout.Label($"IsOnline: {test.IsOnline}");
        GUILayout.Label($"IsServer: {test.IsServer}");
        GUILayout.Label($"IsClient: {test.IsClient}");
        GUILayout.Label($"PlayerID: {test.PlayerID}");
        EditorGUILayout.TextField("JoinCode:", test.JoinCode);
        
        EditorGUILayout.Space();

        GUILayout.Label("Server:", EditorStyles.boldLabel);
        _maxNumberPlayers = EditorGUILayout.IntField("Max Players:", _maxNumberPlayers);
        _allocationRegionIdx = EditorGUILayout.Popup("Allocation Region:", _allocationRegionIdx, test.AllocationRegions ?? Array.Empty<string>());
        if (!test.IsServer && GUILayout.Button("Start Server"))
            test.StartServer(_maxNumberPlayers, test.AllocationRegions[_allocationRegionIdx]);
        if (test.IsServer && GUILayout.Button("Stop Server"))
            test.StopServer();
        
        EditorGUILayout.Space();

        GUILayout.Label("Client:", EditorStyles.boldLabel);
        _joinCode = EditorGUILayout.TextField("Join Code:", _joinCode);
        if (!test.IsClient && GUILayout.Button("Start Client"))
            test.StartClient(_joinCode);
        if (test.IsClient && GUILayout.Button("Stop Client"))
            test.StopClient();
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Data:", EditorStyles.boldLabel);
        if (GUILayout.Button("Register"))
            test.Register();
        if (GUILayout.Button("Unregister"))
            test.Unregister();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_targetClientID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_message"));
        _channel = (ENetworkChannel)EditorGUILayout.EnumPopup(_channel);
        if (GUILayout.Button("Send Message"))
            test.SendToClient(_channel);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif