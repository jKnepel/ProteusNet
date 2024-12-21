using System.Collections.Generic;
using System.Security.Cryptography;
using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Managing;

namespace jKnepel.ProteusNet
{
    public class Objects
    {
        private readonly NetworkManager _networkManager;
        
        private readonly Dictionary<uint, NetworkObject> _networkObjects = new();

        public NetworkObject this[uint i] => _networkObjects[i];
        public bool TryGetValue(uint i, out NetworkObject networkObject) => _networkObjects.TryGetValue(i, out networkObject);
        
        public Objects(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _networkManager.Server.OnLocalServerStarted += SpawnRegisteredObjects;
        }

        public uint GetNextNetworkObjectID()
        {
            uint id;
            do
                id = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
            while (_networkObjects.ContainsKey(id));
            return id;
        }

        public void RegisterNetworkObject(NetworkObject networkObject)
        {
            var collision = _networkObjects.TryAdd(networkObject.ObjectIdentifier, networkObject);
            if (!collision)
                _networkManager.Logger?.LogError($"An Id-collision has occurred for network objects with the Id {networkObject.ObjectIdentifier}");
        }
        
        public void ReleaseNetworkObjectID(uint networkObjectId)
        {
            // TODO : keep id in buffer instead of releasing immediately?
            var success = _networkObjects.Remove(networkObjectId);
            if (!success)
                _networkManager.Logger?.LogError($"The non-existent network object Id {networkObjectId} was attempted to be removed");
        }

        private void SpawnRegisteredObjects()
        {
            foreach (var (_, nobj) in _networkObjects)
            {
                if (!nobj.IsSpawned)
                    nobj.Spawn();
            }
        }
    }
}
