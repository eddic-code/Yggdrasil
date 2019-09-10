#region License

// // /*
// // MIT License
// //
// // Copyright (c) 2019 eddic-code
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy
// // of this software and associated documentation files (the "Software"), to deal
// // in the Software without restriction, including without limitation the rights
// // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// // copies of the Software, and to permit persons to whom the Software is
// // furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all
// // copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// // SOFTWARE.
// //
// // */

#endregion

using System;
using System.Runtime.CompilerServices;
using Yggdrasil.Utility;

namespace Yggdrasil.Coroutines
{
    [AsyncMethodBuilder(typeof(Coroutine<>))]
    public class Coroutine<T> : CoroutineBase, ICriticalNotifyCompletion, IContinuation
    {
        private static readonly ConcurrentPool<Coroutine<T>> _pool = new ConcurrentPool<Coroutine<T>>();

        private bool _isConstant;
        private T _result;
        private IStateMachineWrapper _stateMachine;

        public Coroutine<T> Task => this;

        public bool IsCompleted { get; private set; }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }

        public void Discard()
        {
            if (_isConstant) { return; }

            _result = default;
            IsCompleted = false;

            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }

            _stateMachine = null;

            _pool.Recycle(this);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public static Coroutine<T> Create()
        {
            return _pool.Get();
        }

        public static Coroutine<T> CreateConst(T result)
        {
            var coroutine = _pool.Get();

            coroutine._result = result;
            coroutine.IsCompleted = true;
            coroutine._isConstant = true;

            return coroutine;
        }

        public Coroutine<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            if (_isConstant) { return _result; }

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

            CoroutineManagerBase.CurrentInstance.SetException(exception);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            var wrapper = SmPool.Get<StateMachineWrapper<TStateMachine>>();
            wrapper.StateMachine = stateMachine;

            _stateMachine = wrapper;

            wrapper.MoveNext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter _, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManagerBase.CurrentInstance.AddContinuation(this);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter _,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManagerBase.CurrentInstance.AddContinuation(this);
        }
    }

    [AsyncMethodBuilder(typeof(Coroutine))]
    public class Coroutine : CoroutineBase, ICriticalNotifyCompletion, IContinuation
    {
        private static readonly ConcurrentPool<Coroutine> _pool = new ConcurrentPool<Coroutine>();

        private bool _isConstant;
        private IStateMachineWrapper _stateMachine;

        public Coroutine Task => this;

        public bool IsCompleted { get; private set; }

        public void MoveNext()
        {
            _stateMachine.MoveNext();
        }

        public void Discard()
        {
            if (_isConstant) { return; }

            IsCompleted = false;

            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }

            _stateMachine = null;

            _pool.Recycle(this);
        }

        public void OnCompleted(Action continuation) { }

        public void UnsafeOnCompleted(Action continuation) { }

        public static Coroutine Create()
        {
            return _pool.Get();
        }

        public static Coroutine CreateConst(bool isCompleted)
        {
            var coroutine = _pool.Get();

            coroutine.IsCompleted = isCompleted;
            coroutine._isConstant = true;

            return coroutine;
        }

        public Coroutine GetAwaiter()
        {
            return this;
        }

        public object GetResult()
        {
            if (_isConstant) { return null; }

            IsCompleted = false;
            _pool.Recycle(this);

            return null;
        }

        // This is never called for a lowermost instanced Coroutine, like the CoroutineManager's Yield.
        public void SetResult()
        {
            if (_isConstant) { return; }

            IsCompleted = true;

            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }

            _stateMachine = null;
        }

        public void SetException(Exception exception)
        {
            if (_stateMachine != null) { SmPool.Recycle(_stateMachine); }

            _stateMachine = null;

            CoroutineManagerBase.CurrentInstance.SetException(exception);
        }

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
            CoroutineManagerBase.CurrentInstance.AddContinuation(this);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            CoroutineManagerBase.CurrentInstance.AddContinuation(this);
        }
    }
}