using System;
using System.Collections.Concurrent;

namespace jKnepel.ProteusNet.Utilities
{
    public static class MainThreadQueue
    {
        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();

        static MainThreadQueue()
        {
            StaticGameObject.OnUpdate += UpdateQueue;
        }

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
