using System;
using System.Collections.Generic;

namespace Yggdrasil
{
    // The real awaiter.
    public class CoroutineManager : IDisposable
    {
        private readonly Stack<Action> _continuationsBuffer = new Stack<Action>(100);
        private readonly List<Action> _continuations = new List<Action>(100);
        private readonly Stack<IDiscardable> _buildersBuffer = new Stack<IDiscardable>(100);
        private readonly Dictionary<Type, Action<CoroutineManager>> _onDisposeCallbacks = new Dictionary<Type, Action<CoroutineManager>>(100);

        internal readonly Coroutine Yield;

        internal static CoroutineManager CurrentInstance;

        public CoroutineManager()
        {
            Yield = new Coroutine(this);
        }

        public Node Root { get; set; }

        public long TickCount { get; private set; }

        public void Tick()
        {
            if (Root == null) { return; }

            CurrentInstance = this;

            if (_continuations.Count > 0)
            {
                do
                {
                    var next = _continuations[_continuations.Count - 1];
                    _continuations.RemoveAt(_continuations.Count - 1);

                    next();
                }
                while (_continuationsBuffer.Count == 0 && _continuations.Count > 0);
            }
            else
            {
                Root.Tick();
            }

            ConsumeBuffers();
        }

        public void Reset()
        {
            TickCount = 0;

            // Discard the entire tree's continuations.
            _continuations.Clear();
            _continuationsBuffer.Clear();

            // Recycle all active builders.
            foreach (var builder in _buildersBuffer) { builder.Discard(); }
            _buildersBuffer.Clear();
        }

        public void Dispose()
        {
            foreach (var callback in _onDisposeCallbacks.Values) { callback(this); }
            _onDisposeCallbacks.Clear();

            Reset();
        }

        internal void SetException(Exception exception)
        {
            Reset();

            // Temporary throw. Could log exception instead and continue.
            throw exception;
        }

        internal void AddContinuation(Action continuation)
        {
            _continuationsBuffer.Push(continuation);
        }

        internal void RegisterBuilder(IDiscardable builder)
        {
            _buildersBuffer.Push(builder);
        }

        internal void UnregisterBuilder(IDiscardable builder)
        {
            var previous = _buildersBuffer.Pop();
            if (previous != builder) { throw new Exception("Builder buffer ordering error."); }
        }

        internal void AddDiposeCallback<T>(Action<CoroutineManager> callback)
        {
            _onDisposeCallbacks[typeof(T)] = callback;
        }

        private void ConsumeBuffers()
        {
            foreach (var continuation in _continuationsBuffer) { _continuations.Add(continuation); }
            _continuationsBuffer.Clear();

            if (_continuations.Count == 0) { TickCount++; }
        }
    }
}
