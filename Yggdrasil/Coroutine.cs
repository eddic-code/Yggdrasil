using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    [AsyncMethodBuilder(typeof(Coroutine<>))]
    public class Coroutine<T> : ICriticalNotifyCompletion, INotifyCompletion
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

            CoroutineManager.CurrentInstance.AddContinuation(MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            CoroutineManager.CurrentInstance.AddContinuation(MoveNext);
        }

        private void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }

    [AsyncMethodBuilder(typeof(Coroutine))]
    public class Coroutine : ICriticalNotifyCompletion, INotifyCompletion
    {
        private IAsyncStateMachine _stateMachine;

        public static Coroutine Create()
        {
            return new Coroutine();
        }

        public Coroutine GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted => false;

        public Coroutine Task => this;

        public object GetResult()
        {
            return null;
        }

        public void SetResult()
        {

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

            CoroutineManager.CurrentInstance.AddContinuation(MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;

            CoroutineManager.CurrentInstance.AddContinuation(MoveNext);
        }

        private void MoveNext()
        {
            _stateMachine.MoveNext();
        }
    }
}
