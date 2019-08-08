using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    public class CoroutineMethodBuilder<T>
    {
        private IAsyncStateMachine _stateMachine;
        private Coroutine<T> _coroutine;

        public static CoroutineMethodBuilder<T> Create()
        {
            return new CoroutineMethodBuilder<T>();
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            _stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void SetException(Exception exception)
        {
            _coroutine.SetException(exception);
        }

        public void SetResult(T result)
        {
            _coroutine.SetResult(result);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            awaiter.OnCompleted(MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            awaiter.OnCompleted(MoveNext);
        }

        public Coroutine<T> Task
        {
            get
            {
                _coroutine = Coroutine<T>.Create(CoroutineManager.CurrentInstance);

                return _coroutine;
            }
        }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }

    public class CoroutineMethodBuilder
    {
        private IAsyncStateMachine _stateMachine;

        public static CoroutineMethodBuilder Create()
        {
            return new CoroutineMethodBuilder();
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            _stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void SetException(Exception exception)
        {
            CoroutineManager.CurrentInstance.SetException(exception);
        }

        public void SetResult()
        {
            
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            awaiter.OnCompleted(MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            awaiter.OnCompleted(MoveNext);
        }

        public Coroutine Task => CoroutineManager.CurrentInstance.Yield;

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }
}
