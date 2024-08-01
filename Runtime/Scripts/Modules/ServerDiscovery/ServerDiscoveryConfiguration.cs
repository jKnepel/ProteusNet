using jKnepel.ProteusNet.Managing;
using UnityEngine;

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    [CreateAssetMenu(fileName = "ServerDiscoveryConfiguration", menuName = "ProteusNet/Modules/ServerDiscoveryConfiguration")]
    public class ServerDiscoveryConfiguration : ModuleConfiguration
    {
        public override Module GetModule(INetworkManager networkManager) 
            => new ServerDiscoveryModule(networkManager, this, Settings);
        
        public ServerDiscoverySettings Settings = new();
    }
}
