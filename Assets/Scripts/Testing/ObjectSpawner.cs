using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Managing;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private MonoNetworkManager networkManager;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private Transform parent;

    public void Spawn()
    {
        var nobj = Instantiate(networkObject, parent);
        networkManager.Server.SpawnNetworkObject(nobj);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var test = (ObjectSpawner)target;
        
        if (GUILayout.Button("Spawn"))
            test.Spawn();
    }
}
#endif
