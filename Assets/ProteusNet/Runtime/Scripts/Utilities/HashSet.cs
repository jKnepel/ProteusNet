using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Utilities
{
    [Serializable]
    public class HashSet<T> : ISerializationCallbackReceiver
    {
        private System.Collections.Generic.HashSet<T> _hashSet = new();

        [SerializeField] private List<T> serializedList = new();

        /// <summary>
        /// Adds an item to the HashSet.
        /// </summary>
        public bool Add(T item)
        {
            return _hashSet.Add(item);
        }

        /// <summary>
        /// Removes an item from the HashSet.
        /// </summary>
        public bool Remove(T item)
        {
            return _hashSet.Remove(item);
        }

        /// <summary>
        /// Checks if the HashSet contains a specific item.
        /// </summary>
        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        /// <summary>
        /// Clears the HashSet.
        /// </summary>
        public void Clear()
        {
            _hashSet.Clear();
            serializedList.Clear();
        }

        /// <summary>
        /// Gets the number of elements in the HashSet.
        /// </summary>
        public int Count => _hashSet.Count;

        /// <summary>
        /// Gets the HashSet enumerator.
        /// </summary>
        public System.Collections.Generic.HashSet<T>.Enumerator GetEnumerator()
        {
            return _hashSet.GetEnumerator();
        }

        /// <summary>
        /// Unity's serialization method. Called before serialization.
        /// Converts HashSet to List.
        /// </summary>
        public void OnBeforeSerialize()
        {
            serializedList.Clear();
            serializedList.AddRange(_hashSet);
        }

        /// <summary>
        /// Unity's deserialization method. Called after deserialization.
        /// Converts List back to HashSet.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _hashSet.Clear();
            foreach (T item in serializedList)
            {
                _hashSet.Add(item);
            }
        }

        /// <summary>
        /// Converts the HashSet back to a HashSet.
        /// </summary>
        public System.Collections.Generic.HashSet<T> ToHashSet()
        {
            return new(_hashSet);
        }
        
        /// <summary>
        /// Allows for indexing into the HashSet.
        /// </summary>
        public T this[int index] => serializedList[index];
        
        /// <summary>
        /// Implicit conversion from HashSetWrapper to HashSet.
        /// </summary>
        public static implicit operator System.Collections.Generic.HashSet<T>(HashSet<T> wrapper)
        {
            return wrapper != null ? new System.Collections.Generic.HashSet<T>(wrapper._hashSet) : null;
        }
    }
}
