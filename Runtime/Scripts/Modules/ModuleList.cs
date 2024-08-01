using System;
using System.Collections;
using System.Collections.Generic;

namespace jKnepel.ProteusNet.Modules
{
    public class ModuleList : IList<Module>
    {
        private readonly List<Module> _innerCol = new();
        
#if UNITY_EDITOR
        public event Action<ModuleConfiguration> OnModuleAdded;
        public event Action<ModuleConfiguration> OnModuleRemoved;
        public event Action<int, ModuleConfiguration> OnModuleInserted;
        public event Action<int> OnModuleRemovedAt;
#endif
        
        public IEnumerator<Module> GetEnumerator()
        {
            return _innerCol.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Module item)
        {
            if (item is null) throw new NullReferenceException();
            
            _innerCol.Add(item);
#if UNITY_EDITOR
            OnModuleAdded?.Invoke(item.ModuleConfiguration);
#endif
        }

        public void Clear()
        {
            _innerCol.Clear();
        }

        public bool Contains(Module item)
        {
            if (item is null) throw new NullReferenceException();
            
            return _innerCol.Contains(item);
        }

        public void CopyTo(Module[] array, int arrayIndex)
        {
            _innerCol.CopyTo(array, arrayIndex);
        }
        
        public int IndexOf(Module item)
        {
            if (item is null) throw new NullReferenceException();
            
            return _innerCol.IndexOf(item);
        }

        public bool Remove(Module item)
        {
            if (item is null) throw new NullReferenceException();
            
            var res = _innerCol.Remove(item);
#if UNITY_EDITOR
            OnModuleRemoved?.Invoke(item.ModuleConfiguration);
#endif
            return res;
        }

        public void Insert(int index, Module item)
        {
            if (item is null) throw new NullReferenceException();
            
            _innerCol.Insert(index, item);
#if UNITY_EDITOR
            OnModuleInserted?.Invoke(index, item.ModuleConfiguration);
#endif
        }

        public void RemoveAt(int index)
        {
            _innerCol.RemoveAt(index);
#if UNITY_EDITOR
            OnModuleRemovedAt?.Invoke(index);
#endif
        }

        public Module this[int index]
        {
            get => _innerCol[index];
            set => _innerCol[index] = value;
        }

        public int Count => _innerCol.Count;
        public bool IsReadOnly => false;
    }
}
