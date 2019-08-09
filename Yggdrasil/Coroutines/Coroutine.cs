using System;
using System.Runtime.CompilerServices;
using Yggdrasil.Utility;

namespace Yggdrasil.Coroutines
{
    [AsyncMethodBuilder(typeof(Coroutine<>))]
    public class Coroutine<T> : ICriticalNotifyCompletion, IContinuation
    {
        private IAsyncStateMachine _stateMachine;
        private T _result;

        public static Coroutine<T> Create()
        {
            return new Coroutine<T>();
        }

        public bool IsCompleted => false;

        public Coroutine<T> Task => this;

        public Coroutine<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            return _result;
        }

        public void SetResult(T result)
        {
            _result = result;
        }

        public void SetException(Exception exception)
        {
            CoroutineManager.CurrentInstance.SetException(exception);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            _stateMachine.MoveNext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            CoroutineManager.CurrentInstance.AddContinuation(this);
        }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }

    [AsyncMethodBuilder(typeof(Coroutine))]
    public class Coroutine : ICriticalNotifyCompletion, IContinuation
    {
        private static readonly ConcurrentPool<Coroutine> _pool = new ConcurrentPool<Coroutine>();
        private static readonly ConcurrentPoolMap<IStateMachineWrapper> _smPool = new ConcurrentPoolMap<IStateMachineWrapper>();

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
            if (_stateMachine != null) { _smPool.Recycle(_stateMachine); }
            _stateMachine = null;
        }

        public void SetException(Exception exception)
        {
            if (_stateMachine != null) { _smPool.Recycle(_stateMachine); }
            _stateMachine = null;

            CoroutineManager.CurrentInstance.SetException(exception);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            var wrapper = _smPool.Get<StateMachineWrapper<TStateMachine>>();
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
