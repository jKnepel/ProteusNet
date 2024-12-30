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

    public void Instantiate()
    {
        Instantiate(networkObject, parent);
    }
    
    public void Spawn()
    {
        networkManager.Server.SpawnNetworkObject(networkObject);
    }
    
    public void InstantiateAndSpawn()
    {
        var nobj = Instantiate(networkObject, parent);
        networkManager.Server.SpawnNetworkObject(nobj);
    }

    public void Despawn()
    {
        networkManager.Server.DespawnNetworkObject(networkObject);
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
        
        if (GUILayout.Button("Instantiate"))
            test.Instantiate();
        
        if (GUILayout.Button("Spawn"))
            test.Spawn();
        
        if (GUILayout.Button("Instantiate and Spawn"))
            test.InstantiateAndSpawn();
        
        if (GUILayout.Button("Despawn"))
            test.Despawn();
    }
}
#endif
