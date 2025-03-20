using jKnepel.ProteusNet.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

public class KatamariManager : NetworkBehaviour
{
	#region attributes

	[Header("References")]
	[SerializeField] private MonoNetworkManager networkManager;
	[SerializeField] private Transform objectParent;
	[SerializeField] private NetworkObject objectPrefab;
	[SerializeField] private NetworkObject playerPrefab;

	[Header("Values")]
	[SerializeField] private int numberOfObjects = 49;
	[SerializeField] private float spawnDistance = 1f;

	private NetworkObject[] _networkObjects;
	private readonly Dictionary<uint, NetworkObject> _playerObjects = new();

	#endregion

	#region lifecycle

	public override void OnServerSpawned()
	{
		networkManager.Server.OnRemoteClientConnected += SpawnClient;
		networkManager.Server.OnRemoteClientDisconnected += DespawnClient;
		
		if (objectParent == null)
			objectParent = transform;

		_networkObjects = new NetworkObject[numberOfObjects];
		var numberOfColumns = (int)Math.Ceiling(Mathf.Sqrt(numberOfObjects));
		var numberOfRows = (int)Math.Ceiling((float)numberOfObjects / numberOfColumns);
		var startX = -((float)(numberOfColumns - 1) / 2 * spawnDistance);
		var startZ = -((float)(numberOfRows    - 1) / 2 * spawnDistance);

		for (var index = 0; index < numberOfObjects; index++)
		{
			var i = index / numberOfRows;
			var j = index % numberOfRows;

			var x = startX + i * spawnDistance;
			var z = startZ + j * spawnDistance;
			Vector3 position = new(x, objectPrefab.transform.position.y, z);
			var obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation, objectParent);
			obj.Spawn();
			_networkObjects[index] = obj;
		}
	}  

	public override void OnServerDespawned()
	{
		networkManager.Server.OnRemoteClientConnected -= SpawnClient;
		networkManager.Server.OnRemoteClientDisconnected -= DespawnClient;
	}

	#endregion
	
	#region private methods

	private void SpawnClient(uint clientID)
	{
		var numOfPlayers = networkManager.Server.ConnectedClients.Count;
		var player = Instantiate(playerPrefab, new(-3f + numOfPlayers * 1.5f, 0.5f, -5), Quaternion.identity);
		player.Spawn(clientID);
		_playerObjects.Add(clientID, player);
	}

	private void DespawnClient(uint clientID)
	{
		if (_playerObjects.Remove(clientID, out var player))
			Destroy(player.gameObject);
	}
	
	#endregion
}
