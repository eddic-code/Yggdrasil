using System;
using System.Collections.Generic;

namespace Yggdrasil
{
    // The real awaiter.
    public class CoroutineManager
    {
        private readonly Stack<IContinuation> _continuationsBuffer = new Stack<IContinuation>(100);
        private readonly List<IContinuation> _continuations = new List<IContinuation>(100);

        internal readonly Coroutine Yield;

        internal static CoroutineManager CurrentInstance;

        public CoroutineManager()
        {
            Yield = new Coroutine();
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

                    next.MoveNext();
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
        }

        internal void SetException(Exception exception)
        {
            Reset();

            // Temporary throw. Could log exception instead and continue.
            throw exception;
        }

        internal void AddContinuation(IContinuation continuation)
        {
            _continuationsBuffer.Push(continuation);
        }

        private void ConsumeBuffers()
        {
            foreach (var continuation in _continuationsBuffer) { _continuations.Add(continuation); }
            _continuationsBuffer.Clear();

            if (_continuations.Count == 0) { TickCount++; }
        }
    }
}
