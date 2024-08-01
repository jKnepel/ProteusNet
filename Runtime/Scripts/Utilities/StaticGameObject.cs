using System;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace jKnepel.ProteusNet.Utilities
{
    public static class StaticGameObject
    {
        public static event Action OnUpdate;
        
#if UNITY_EDITOR
        static StaticGameObject()
        {
            EditorApplication.update += () => OnUpdate?.Invoke();
        }
#else
        static StaticGameObject()
        {
            UnityMainThreadHook.Instance.OnUpdate += () => OnUpdate?.Invoke();
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
    }
}
