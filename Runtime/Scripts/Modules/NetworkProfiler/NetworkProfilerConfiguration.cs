using jKnepel.ProteusNet.Managing;
using UnityEngine;

namespace jKnepel.ProteusNet.Modules.NetworkProfiler
{
    [CreateAssetMenu(fileName = "NetworkProfilerConfiguration", menuName = "ProteusNet/Modules/NetworkProfilerConfiguration")]
    public class NetworkProfilerConfiguration : ModuleConfiguration
    {
        public override Module GetModule(INetworkManager networkManager) 
            => new NetworkProfilerModule(networkManager, this, Settings);
        
        public NetworkProfilerSettings Settings = new();
    }
}
