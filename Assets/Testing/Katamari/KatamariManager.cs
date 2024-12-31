using System;
using jKnepel.ProteusNet.Components;
using UnityEngine;

public class KatamariManager : NetworkBehaviour
{
	#region attributes

	[Header("References")]
	[SerializeField] private MonoNetworkManager networkManager;
	[SerializeField] private Transform objectParent;
	[SerializeField] private NetworkTransform objectPrefab;

	[Header("Values")]
	[SerializeField] private int numberOfObjects = 50;
	[SerializeField] private float spawnDistance = 2.0f;

	private NetworkTransform[] _networkObjects;

	#endregion

	#region lifecycle

	public override void OnServerStarted()
	{
		base.OnServerStarted();
		
		if (objectParent == null)
			objectParent = transform;

		_networkObjects = new NetworkTransform[numberOfObjects];
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
				obj.NetworkObject.Spawn();
				_networkObjects[index-1] = obj;
				index++;
			}
		}
	}

	#endregion
}
