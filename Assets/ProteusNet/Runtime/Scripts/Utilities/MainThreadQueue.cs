using System;
using System.Collections.Concurrent;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace jKnepel.ProteusNet.Utilities
{
    public static class MainThreadQueue
    {
        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();

#if UNITY_EDITOR
        static MainThreadQueue()
        {
            EditorApplication.update += UpdateQueue;
        }
#else
        [RuntimeInitializeOnLoadMethod]
        private static void InitRuntime()
        {
            UnityMainThreadHook.Instance.OnUpdate += UpdateQueue;
        }
        
        private class UnityMainThreadHook : MonoBehaviour
        {
            public event Action OnUpdate;

            private static UnityMainThreadHook _instance;
            public static UnityMainThreadHook Instance
            {
                get
                {
                    if (_instance != null) return _instance;

                    GameObject singletonObject = new() { hideFlags = HideFlags.HideAndDontSave };
                    _instance = singletonObject.AddComponent<UnityMainThreadHook>();
                    DontDestroyOnLoad(singletonObject);

                    return _instance;
                }
            }

            private void Update()
            {
                OnUpdate?.Invoke();
            }
        }
#endif

        private static void UpdateQueue()
        {
            while (_mainThreadQueue.Count > 0)
			{
                _mainThreadQueue.TryDequeue(out var action);
                action?.Invoke();
			}
        }

        public static void Enqueue(Action action)
		{
            _mainThreadQueue.Enqueue(action);
		}
    }
}
