using System;
using System.Runtime.CompilerServices;
using Yggdrasil.Utility;

namespace Yggdrasil.Coroutines
{
    [AsyncMethodBuilder(typeof(Coroutine<>))]
    public class Coroutine<T> : CoroutineBase, ICriticalNotifyCompletion, IContinuation
    {
        private static readonly ConcurrentPool<Coroutine<T>> _pool = new ConcurrentPool<Coroutine<T>>();

        private IStateMachineWrapper _stateMachine;
        private T _result;

        public static Coroutine<T> Create()
        {
            return _pool.Get();
        }

        public static Coroutine<T> CreateWith(T result)
        {
            var coroutine = _pool.Get();

            coroutine._result = result;
            coroutine.IsCompleted = true;

            return coroutine;
        }

        public bool IsCompleted { get; private set; }

        public Coroutine<T> Task => this;

        public Coroutine<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            var result = _result;

            _result = default;
            IsCompleted = false;
            _pool.Recycle(this);

            return result;
        }

        public void SetResult(T result)
        {
            _result = result;
            IsCompleted = true;

            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }
            _stateMachine = null;
        }

        public void SetException(Exception exception)
        {
            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }
            _stateMachine = null;

            CoroutineManager.CurrentInstance.SetException(exception);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            var wrapper = SmPool.Get<StateMachineWrapper<TStateMachine>>();
            wrapper.StateMachine = stateMachine;

            _stateMachine = wrapper;

            wrapper.MoveNext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }

    [AsyncMethodBuilder(typeof(Coroutine))]
    public class Coroutine : CoroutineBase, ICriticalNotifyCompletion, IContinuation
    {
        private static readonly ConcurrentPool<Coroutine> _pool = new ConcurrentPool<Coroutine>();

        private IStateMachineWrapper _stateMachine;

        public static Coroutine Create()
        {
            return _pool.Get();
        }

        public Coroutine GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted => false;

        public Coroutine Task => this;

        public object GetResult()
        {
            if (CoroutineManager.CurrentInstance.Yield != this)
            {
                _pool.Recycle(this);
            }

            return null;
        }

        public void SetResult()
        {
            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }
            _stateMachine = null;
        }

        public void SetException(Exception exception)
        {
            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }
            _stateMachine = null;

            CoroutineManager.CurrentInstance.SetException(exception);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            var wrapper = SmPool.Get<StateMachineWrapper<TStateMachine>>();
            wrapper.StateMachine = stateMachine;

            _stateMachine = wrapper;

            wrapper.MoveNext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }
}
