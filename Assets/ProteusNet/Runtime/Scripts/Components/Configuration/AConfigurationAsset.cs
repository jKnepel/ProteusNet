using UnityEngine;

namespace jKnepel.ProteusNet.Components.Configuration
{
    public abstract class AConfigurationAsset<T> : ScriptableObject where T : class
    {
        [SerializeReference] protected T value;
        public T Value => value;

        private void Awake() => EnsureInitialized();
        private void OnValidate() => EnsureInitialized();
        private void Reset() => EnsureInitialized();
        private void EnsureInitialized() => value ??= CreateInstance();
        protected abstract T CreateInstance();
    }
}
