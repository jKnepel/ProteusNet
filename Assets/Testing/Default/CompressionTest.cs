using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Serializing;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CompressionTest : MonoBehaviour
{
    [SerializeField] private MonoNetworkManager _manager;
    [SerializeField] private SerializerSettings _serializerSettings;
    [SerializeField] private uint _targetClientID;
    
    public bool IsOnline => _manager.IsOnline;
    public bool IsServer => _manager.IsServer;
    public bool IsClient => _manager.IsClient;
    public bool IsHost => _manager.IsHost;

    public void StartServer()
    {
        _manager.StartServer();
    }

    public void StopServer()
    {
        _manager.StopServer();
    }

    public void StartClient()
    {
        _manager.StartClient();
    }

    public void StopClient()
    {
        _manager.StopClient();
    }

    public void Register()
    {
        _manager.Client.RegisterByteData("values", ReceiveValueBytes);
    }

    public void Unregister()
    {
        _manager.Client.UnregisterByteData("values", ReceiveValueBytes);
    }

    public void SendValuesToClient(ENetworkChannel channel)
    {
        ValueStruct data = new()
        {
            Byte = 1,
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123
        };

        Writer writer = new(_serializerSettings);
        writer.Write(data);
        _manager.Client.SendByteDataToClient(_targetClientID, "values", writer.GetBuffer(), channel);
    }

    private void ReceiveValueBytes(ByteData data)
    {
        Reader reader = new(data.Data, _serializerSettings);
        var message = reader.Read<ValueStruct>();
        
        Debug.Log($"Received {data.Data.Length} bytes from {data.SenderID} during tick {data.Tick} at {data.Timestamp}:\n" +
                  $"Byte = {message.Byte},\n" +
                  $"Short = {message.Short},\n" +
                  $"UShort = {message.UShort},\n" +
                  $"Int = {message.Int},\n" +
                  $"UInt = {message.UInt},\n" +
                  $"Long = {message.Long},\n" +
                  $"ULong = {message.ULong}");
    }

    private struct ValueStruct
    {
        public byte Byte;
        public short Short;
        public ushort UShort;
        public int Int;
        public uint UInt;
        public long Long;
        public ulong ULong;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CompressionTest))]
public class CompressionTestEditor : Editor
{
    private ENetworkChannel _channel = ENetworkChannel.ReliableOrdered;
    
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

        var test = (CompressionTest)target;
        
        GUILayout.Label($"IsOnline: {test.IsOnline}");
        GUILayout.Label($"IsServer: {test.IsServer}");
        GUILayout.Label($"IsClient: {test.IsClient}");
        GUILayout.Label($"IsHost: {test.IsHost}");
        _channel = (ENetworkChannel)EditorGUILayout.EnumPopup(_channel);
        
        if (GUILayout.Button("Register"))
            test.Register();
        if (GUILayout.Button("Unregister"))
            test.Unregister();
        if (GUILayout.Button("Start Server"))
            test.StartServer();
        if (GUILayout.Button("Stop Server"))
            test.StopServer();
        if (GUILayout.Button("Start Client"))
            test.StartClient();
        if (GUILayout.Button("Stop Client"))
            test.StopClient();
        if (GUILayout.Button("Send Values"))
            test.SendValuesToClient(_channel);
    }
}
#endif