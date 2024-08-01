using jKnepel.ProteusNet.Managing;
using UnityEngine;

namespace jKnepel.ProteusNet.Modules
{
    public abstract class ModuleConfiguration : ScriptableObject
    {
        public abstract Module GetModule(INetworkManager networkManager);
    }
}
