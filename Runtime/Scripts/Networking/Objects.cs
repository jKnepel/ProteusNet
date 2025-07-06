using jKnepel.ProteusNet.Components;
using jKnepel.ProteusNet.Managing;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace jKnepel.ProteusNet
{
    public class Objects
    {
        private readonly NetworkManager _networkManager;
        
        private readonly Dictionary<uint, NetworkObject> _networkObjects = new();

        public NetworkObject this[uint i] => _networkObjects[i];
        public bool TryGetValue(uint i, out NetworkObject networkObject) => _networkObjects.TryGetValue(i, out networkObject);
        public List<NetworkObject> NetworkObjects => _networkObjects.Values.ToList();
        
        public Objects(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public uint GetNextNetworkObjectID()
        {
            uint id;
            do
                id = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
            while (_networkObjects.ContainsKey(id));
            return id;
        }

        public bool RegisterNetworkObject(NetworkObject networkObject)
        {
            return _networkObjects.TryAdd(networkObject.ObjectIdentifier, networkObject);
        }
        
        public bool ReleaseNetworkObjectID(uint networkObjectId)
        {
            // TODO : keep id in buffer instead of releasing immediately?
            return _networkObjects.Remove(networkObjectId);
        }
    }
}
