using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Yggdrasil.Enums;

namespace Yggdrasil
{
    // The real awaiter.
    public class CoroutineManager
    {
        private readonly Stack<Action> _buffer = new Stack<Action>(100);
        private readonly List<Action> _continuations = new List<Action>(100);

        internal readonly Coroutine Yield;

        internal static CoroutineManager CurrentInstance;

        public CoroutineManager()
        {
            Yield = new Coroutine(this);
        }

        public Node Root { get; set; }

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
                while (_buffer.Count == 0 && _continuations.Count > 0);
            }
            else
            {
                Root.Tick();
            }

            foreach (var continuation in _buffer) { _continuations.Add(continuation); }
            _buffer.Clear();
        }

        internal void SetException(Exception exception)
        {
            _continuations.Clear();
            _buffer.Clear();

            throw exception;
        }

        internal void OnCompleted(Action continuation)
        {
            _buffer.Push(continuation);
        }

        internal void UnsafeOnCompleted(Action continuation)
        {
            _buffer.Push(continuation);
        }
    }
}
