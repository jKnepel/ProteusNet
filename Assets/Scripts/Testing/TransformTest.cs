using jKnepel.ProteusNet.Managing;
using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Serialising;
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
        if (_sendObject.hasChanged && _synchronise)
        {
            SendTransformToClient(ENetworkChannel.UnreliableOrdered);
            _sendObject.hasChanged = false;
        }
	}

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
        _manager.Client.RegisterByteData("transform", ReceiveTransformStruct);
    }

    public void Unregister()
    {
        _manager.Client.UnregisterByteData("transform", ReceiveTransformStruct);
    }

    public void SendTransformToClient(ENetworkChannel channel)
    {
        var data = new TransformStruct
        {
            Position = _sendObject.position,
            Rotation = _sendObject.rotation
        };
        
        Writer writer = new(_serialiserConfiguration.Settings);
        writer.Write(data);
        _manager.Client.SendByteDataToClient(_targetClientID, "transform", writer.GetBuffer(), channel);
    }

    private void ReceiveTransformStruct(ByteData data)
    {
        Reader reader = new(data.Data, _serialiserConfiguration.Settings);
        var message = reader.Read<TransformStruct>();
        
        Debug.Log(data.Data.Length);
        _receiveObject.SetPositionAndRotation(message.Position, message.Rotation);
    }

    private struct TransformStruct
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