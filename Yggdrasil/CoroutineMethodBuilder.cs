using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    public class CoroutineMethodBuilder<T> : IDiscardable
    {
        private static readonly ConcurrentStack<CoroutineMethodBuilder<T>> _pool = new ConcurrentStack<CoroutineMethodBuilder<T>>();

        private IAsyncStateMachine _stateMachine;
        private bool _isDiscarded;

        public static CoroutineMethodBuilder<T> Create()
        {
            // We pool method builders to avoid allocations.
            if (!_pool.TryPop(out var builder))
            {
                builder = new CoroutineMethodBuilder<T>();
            }

            builder._isDiscarded = false;
            CoroutineManager.CurrentInstance.RegisterBuilder(builder);

            return builder;
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            _stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        // Last call made to the builder.
        public void SetException(Exception exception)
        {
            CoroutineManager.CurrentInstance.SetException(exception);
        }

        // Last call made to the builder.
        public void SetResult(T result)
        {
            Coroutine<T>.SetResult(result);

            CoroutineManager.CurrentInstance.UnregisterBuilder(this);

            Discard();
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

        public Coroutine<T> Task { get; } = new Coroutine<T>();

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }

        public void Discard()
        {
            if (_isDiscarded) { return; }
            _isDiscarded = true;

            // Should we null the state machine too?
            // It's an object when in Debug, a struct when in Release.

            #if DEBUG
            _stateMachine = null;
            #endif

            _pool.Push(this);
        }
    }

    public class CoroutineMethodBuilder : IDiscardable
    {
        private static readonly ConcurrentStack<CoroutineMethodBuilder> _pool = new ConcurrentStack<CoroutineMethodBuilder>();

        private IAsyncStateMachine _stateMachine;
        private bool _isDiscarded;

        public static CoroutineMethodBuilder Create()
        {
            if (!_pool.TryPop(out var builder))
            {
                builder = new CoroutineMethodBuilder();
            }

            builder._isDiscarded = false;
            CoroutineManager.CurrentInstance.RegisterBuilder(builder);

            return builder;
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _stateMachine = stateMachine;
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        // Last call made to the builder.
        public void SetException(Exception exception)
        {
            CoroutineManager.CurrentInstance.SetException(exception);
        }

        // Last call made to the builder.
        public void SetResult()
        {
            CoroutineManager.CurrentInstance.UnregisterBuilder(this);

            Discard();
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

        public Coroutine Task => CoroutineManager.CurrentInstance.Yield;

        public void Discard()
        {
            if (_isDiscarded) { return; }
            _isDiscarded = true;

            // Should we null the state machine too?
            // It's an object when in Debug, a struct when in Release.

            #if DEBUG
            _stateMachine = null;
            #endif

            _pool.Push(this);
        }
    }
}
