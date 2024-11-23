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
        }

        public uint GetNextNetworkObjectID(NetworkObject networkObject)
        {
            uint id;
            do
            {
                id = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
                id |= 1u << 31;
            }
            while (!_networkObjects.TryAdd(id, networkObject));
            return id;
        }

        public void RegisterNetworkObjectID(uint networkObjectId, NetworkObject networkObject)
        {
            var collision = _networkObjects.TryAdd(networkObjectId, networkObject);
            if (!collision)
                _networkManager.Logger?.LogError($"An Id-collision has occurred for network objects with the Id {networkObjectId}");
        }
        
        public void ReleaseNetworkObjectID(uint networkObjectId)
        {
            var success = _networkObjects.Remove(networkObjectId);
            if (!success)
                _networkManager.Logger?.LogError($"The non-existent network object Id {networkObjectId} was attempted to be removed");
        }
    }
}
