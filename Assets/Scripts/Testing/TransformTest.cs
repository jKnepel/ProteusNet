using jKnepel.SimpleUnityNetworking.Managing;
using jKnepel.SimpleUnityNetworking.Networking;
using jKnepel.SimpleUnityNetworking.Serialising;
using jKnepel.SimpleUnityNetworking.SyncDataTypes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TransformTest : MonoBehaviour
{
    [SerializeField] private MonoNetworkManager _manager;
    [SerializeField] private SerialiserConfiguration _serialiserConfiguration;
    [SerializeField] private uint _targetClientID;
    [SerializeField] private bool _synchronise;
    [SerializeField] private Transform _sendObject;
    [SerializeField] private Transform _receiveObject;
    
    public bool IsOnline => _manager?.IsOnline ?? false;
    public bool IsServer => _manager?.IsServer ?? false;
    public bool IsClient => _manager?.IsClient ?? false;
    public bool IsHost => _manager?.IsHost ?? false;

    private void Update()
    {
        _manager.Update();
        if (_sendObject.hasChanged && _synchronise)
        {
            SendTransformToClient(ENetworkChannel.UnreliableOrdered);
            _sendObject.hasChanged = false;
        }
	}

    public void StartServer()
    {
        _manager.StartServer("server");
    }

    public void StopServer()
    {
        _manager.StopServer();
    }

    public void StartClient()
    {
        _manager.StartClient("user", new());
    }

    public void StopClient()
    {
        _manager.StopClient();
    }

    public void Register()
    {
        _manager.RegisterByteData("transform", ReceiveTransformStruct);
    }

    public void Unregister()
    {
        _manager.UnregisterByteData("transform", ReceiveTransformStruct);
    }

    public void SendTransformToClient(ENetworkChannel channel)
    {
        var data = new TransformStruct
        {
            Position = _sendObject.position,
            Rotation = _sendObject.rotation
        };
        
        Writer writer = new(_serialiserConfiguration);
        writer.Write(data);
        _manager.SendByteDataToClient(_targetClientID, "transform", writer.GetBuffer(), channel);
    }

    private void ReceiveTransformStruct(uint clientID, byte[] data)
    {
        Reader reader = new(data, _serialiserConfiguration);
        var message = reader.Read<TransformStruct>();
        
        Debug.Log(data.Length);
        _receiveObject.SetPositionAndRotation(message.Position, message.Rotation);
    }

    private struct TransformStruct : IStructData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TransformTest))]
public class TransformTestEditor : Editor
{
    private ENetworkChannel _channel = ENetworkChannel.ReliableOrdered;
    
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

        var test = (TransformTest)target;
        
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
        if (GUILayout.Button("Send Transform"))
            test.SendTransformToClient(_channel);
    }
}
#endif