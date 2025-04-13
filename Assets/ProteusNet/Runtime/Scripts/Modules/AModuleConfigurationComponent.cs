using jKnepel.ProteusNet.Components;
using UnityEngine;

namespace jKnepel.ProteusNet.Modules
{
    [RequireComponent(typeof(MonoNetworkManager))]
    public abstract class AModuleConfigurationComponent<T> : MonoBehaviour where T : Module
    {
        [SerializeReference] protected T value;
        public T Value => value;

        protected void Awake()
        {
            EnsureInitialized();
            Value.Initialize(GetComponent<MonoNetworkManager>());
        }
        
        private void OnValidate() => EnsureInitialized();
        private void Reset() => EnsureInitialized();
        private void EnsureInitialized() => value ??= CreateInstance();
        protected abstract T CreateInstance();
    }
}
