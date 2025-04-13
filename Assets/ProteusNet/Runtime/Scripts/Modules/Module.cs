using jKnepel.ProteusNet.Managing;
using System;

namespace jKnepel.ProteusNet.Modules
{
    public abstract class Module : IDisposable
    {
        public abstract string Name { get; }

        public INetworkManager NetworkManager { get; private set; }

        public void Initialize(INetworkManager networkManager)
        {
            NetworkManager = networkManager;
            Initialize();
        }

        protected virtual void Initialize() {}
        
        ~Module()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
