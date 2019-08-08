using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    // The awaitable task type that also acts as its awaiter.
    // We recycle the generic types to reduce allocations.
    [AsyncMethodBuilder(typeof(CoroutineMethodBuilder<>))]
    public class Coroutine<T> : ICriticalNotifyCompletion, INotifyCompletion, IDiscardable
    {
        private static readonly ConcurrentStack<Coroutine<T>> _pool = new ConcurrentStack<Coroutine<T>>();

        private bool _isDiscarded;

        public T Result;
        public CoroutineManager Manager;

        public static Coroutine<T> Create(CoroutineManager manager)
        {
            if (!_pool.TryPop(out var coroutine))
            {
                coroutine = new Coroutine<T>(manager);
            }

            coroutine.Manager = manager;
            coroutine._isDiscarded = false;

            return coroutine;
        }

        public Coroutine(CoroutineManager manager)
        {
            Manager = manager;
        }

        public bool IsCompleted => false;

        public Coroutine<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            return Result;
        }

        public void SetException(Exception exception)
        {
            Manager.SetException(exception);
        }

        public void SetResult(T result)
        {
            Result = result;
        }

        public void OnCompleted(Action continuation)
        {
            Manager.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Manager.OnCompleted(continuation);
        }

        public void Discard()
        {
            if (_isDiscarded) { return; }
            _isDiscarded = true;

            Result = default;
            Manager = null;

            _pool.Push(this);
        }
    }

    // No need to recycle this since there's only one instance per manager that gets reused.
    [AsyncMethodBuilder(typeof(CoroutineMethodBuilder))]
    public class Coroutine : ICriticalNotifyCompletion, INotifyCompletion
    {
        public CoroutineManager Manager;

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
            Manager.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Manager.OnCompleted(continuation);
        }
    }
}
