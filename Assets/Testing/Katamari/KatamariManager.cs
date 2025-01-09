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
	[SerializeField] private int numberOfObjects = 50;
	[SerializeField] private float spawnDistance = 2.0f;

	private NetworkObject[] _networkObjects;
	private Dictionary<uint, NetworkObject> _playerObjects = new();

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
		var remainder = numberOfObjects % numberOfRows;
		var startX = -((float)(numberOfColumns - 1) / 2 * spawnDistance);
		var startZ = -((float)(numberOfRows    - 1) / 2 * spawnDistance);

		var index = 1;
		for (var i = 0; i < numberOfColumns; i++)
		{
			for (var j = 0; j < numberOfRows; j++)
			{
				if (remainder > 0 && i == numberOfColumns - 1 && j >= remainder)
					return;

				var x = startX + i * spawnDistance;
				var z = startZ + j * spawnDistance;
				Vector3 position = new(x, objectPrefab.transform.position.y, z);
				var obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation, objectParent);
				obj.Spawn();
				_networkObjects[index-1] = obj;
				index++;
			}
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
		var player = Instantiate(playerPrefab, new(0, 5, -5), Quaternion.identity);
		player.Spawn(clientID);
		_playerObjects.Add(clientID, player);
	}

	private void DespawnClient(uint clientID)
	{
		var player = _playerObjects[clientID];
		Destroy(player.gameObject);
	}
	
	
	#endregion
}
