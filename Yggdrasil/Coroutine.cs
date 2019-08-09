using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    // The awaitable task type that also acts as its awaiter.
    // We recycle the generic types to reduce allocations.
    [AsyncMethodBuilder(typeof(CoroutineMethodBuilder<>))]
    public struct Coroutine<T> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // Dictionary can be non-concurrent since only one thread should access a manager key at a time.
        private static readonly Dictionary<CoroutineManager, T> _results = new Dictionary<CoroutineManager, T>();

        public bool IsCompleted => false;

        public Coroutine<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            return _results[CoroutineManager.CurrentInstance];
        }

        public static void SetResult(T result)
        {
            var manager = CoroutineManager.CurrentInstance;

            if (!_results.ContainsKey(manager))
            {
                manager.AddDiposeCallback<T>(OnManagerDisposed);
            }

            _results[manager] = result;
        }

        public void OnCompleted(Action continuation)
        {
            CoroutineManager.CurrentInstance.AddContinuation(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            CoroutineManager.CurrentInstance.AddContinuation(continuation);
        }

        private static void OnManagerDisposed(CoroutineManager manager)
        {
            _results.Remove(manager);
        }
    }

    // No need to recycle this since there's only one instance per manager that gets reused.
    [AsyncMethodBuilder(typeof(CoroutineMethodBuilder))]
    public struct Coroutine : ICriticalNotifyCompletion, INotifyCompletion
    {
        public readonly CoroutineManager Manager;

        public Coroutine(CoroutineManager manager)
        {
            Manager = manager;
        }

        public Coroutine GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted => false;

        public void SetException(Exception exception)
        {
            Manager.SetException(exception);
        }

        public object GetResult()
        {
            return null;
        }

        public void SetResult()
        {

        }

        public void OnCompleted(Action continuation)
        {
            Manager.AddContinuation(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Manager.AddContinuation(continuation);
        }
    }
}
