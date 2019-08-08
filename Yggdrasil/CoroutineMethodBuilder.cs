using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yggdrasil
{
    public class CoroutineMethodBuilder<T> : IDiscardable
    {
        private static readonly ConcurrentStack<CoroutineMethodBuilder<T>> _pool = new ConcurrentStack<CoroutineMethodBuilder<T>>();

        private IAsyncStateMachine _stateMachine;
        private Coroutine<T> _coroutine;
        private bool _isDiscarded;

        public static CoroutineMethodBuilder<T> Create()
        {
            // We pool method builders to avoid allocations.
            if (!_pool.TryPop(out var builder))
            {
                builder = new CoroutineMethodBuilder<T>();
            }

            builder._isDiscarded = false;

            return builder;
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

        // Last call made to the builder.
        public void SetException(Exception exception)
        {
            _coroutine.SetException(exception);

            Discard();
        }

        // Last call made to the builder.
        public void SetResult(T result)
        {
            _coroutine.SetResult(result);

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

        public void Discard()
        {
            if (_isDiscarded) { return; }
            _isDiscarded = true;

            _coroutine = null;

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
            // We pool method builders to avoid allocations.
            if (!_pool.TryPop(out var builder))
            {
                builder = new CoroutineMethodBuilder();
            }

            builder._isDiscarded = false;

            return builder;
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

        // Last call made to the builder.
        public void SetException(Exception exception)
        {
            CoroutineManager.CurrentInstance.SetException(exception);

            Discard();
        }

        // Last call made to the builder.
        public void SetResult()
        {
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

        public Coroutine Task => CoroutineManager.CurrentInstance.Yield;

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
}
