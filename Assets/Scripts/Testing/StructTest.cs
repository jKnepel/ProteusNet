using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StructTest : MonoBehaviour
{
    [SerializeField] private MonoNetworkManager _manager;
    
    public bool IsOnline => _manager?.IsOnline ?? false;
    public bool IsServer => _manager?.IsServer ?? false;
    public bool IsClient => _manager?.IsClient ?? false;
    public bool IsHost => _manager?.IsHost ?? false;

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
    
    public void RegisterServer()
    {
        _manager.Server.RegisterStructData<MessageStruct>(ReceiveStruct);
    }

    public void UnregisterServer()
    {
        _manager.Server.UnregisterStructData<MessageStruct>(ReceiveStruct);
    }

    public void RegisterClient()
    {
        _manager.Client.RegisterStructData<MessageStruct>(ReceiveStruct);
    }

    public void UnregisterClient()
    {
        _manager.Client.UnregisterStructData<MessageStruct>(ReceiveStruct);
    }
    
    public void SendToClientFromServer(uint client, string message, ENetworkChannel channel = ENetworkChannel.ReliableOrdered)
    {
        MessageStruct str = new()
        {
            String = message,
            Byte = 1,
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123,
            Ints = new [] { 1, 2, 3 }
        };
        _manager.Server.SendStructDataToClient(client, str, channel);
    }

    public void SendToClient(uint client, string message, ENetworkChannel channel = ENetworkChannel.ReliableOrdered)
    {
        MessageStruct str = new()
        {
            String = message,
            Byte = 1,
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123,
            Ints = new [] { 1, 2, 3 }
        };
        _manager.Client.SendStructDataToClient(client, str, channel);
    }

    public void SendToServer(string message, ENetworkChannel channel = ENetworkChannel.ReliableOrdered)
    {
        MessageStruct str = new()
        {
            String = message,
            Byte = 1,
            Short = -2,
            UShort = 5,
            Int = -998,
            UInt = 213,
            Long = -12313123,
            ULong = 123123,
            Ints = new [] { 1, 2, 3 }
        };
        _manager.Client.SendStructDataToServer(str, channel);
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
                  $"Ints = {string.Join(",", data.Data.Ints)},\n");
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
[CustomEditor(typeof(StructTest))]
public class StructTestEditor : Editor
{
    private uint _clientID = 1;
    private string _message = string.Empty;
    private ENetworkChannel _channel = ENetworkChannel.ReliableOrdered;
    
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

        var test = (StructTest)target;
        
        GUILayout.Label($"IsOnline: {test.IsOnline}");
        GUILayout.Label($"IsServer: {test.IsServer}");
        GUILayout.Label($"IsClient: {test.IsClient}");
        GUILayout.Label($"IsHost: {test.IsHost}");
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Register Server"))
            test.RegisterServer();
        if (GUILayout.Button("Unregister Server"))
            test.UnregisterServer();
        if (GUILayout.Button("Register Client"))
            test.RegisterClient();
        if (GUILayout.Button("Unregister Client"))
            test.UnregisterClient();
        
        EditorGUILayout.Space();
        
        if (!test.IsServer && GUILayout.Button("Start Server"))
            test.StartServer();
        if (test.IsServer && GUILayout.Button("Stop Server"))
            test.StopServer();
        if (!test.IsClient && GUILayout.Button("Start Client"))
            test.StartClient();
        if (test.IsClient && GUILayout.Button("Stop Client"))
            test.StopClient();
        
        EditorGUILayout.Space();

        _clientID = (uint)EditorGUILayout.IntField("Client ID:", (int)_clientID);
        _message = EditorGUILayout.TextField("Mesage:", _message);
        _channel = (ENetworkChannel)EditorGUILayout.EnumPopup("Channel:", _channel);
        if (GUILayout.Button("Send Message To Client From Server"))
            test.SendToClientFromServer(_clientID, _message, _channel);
        if (GUILayout.Button("Send Message To Client"))
            test.SendToClient(_clientID, _message, _channel);
        if (GUILayout.Button("Send Message To Server"))
            test.SendToServer(_message, _channel);
    }
}
#endif