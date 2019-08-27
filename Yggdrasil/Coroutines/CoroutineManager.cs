using System;
using System.Collections.Generic;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Coroutines
{
    // The real awaiter.
    public class CoroutineManager
    {
        private readonly Stack<IContinuation> _continuationsBuffer = new Stack<IContinuation>(100);
        private readonly List<IContinuation> _continuations = new List<IContinuation>(100);
        private readonly Stack<Node> _active = new Stack<Node>(100);

        private Coroutine<Result> _rootCoroutine;

        internal readonly Coroutine Yield;

        [ThreadStatic]
        internal static CoroutineManager CurrentInstance;

        public event EventHandler<Node> NodeActiveEventHandler;

        public event EventHandler<Node> NodeInactiveEventHandler;

        public CoroutineManager()
        {
            Yield = new Coroutine();
        }

        public Node Root { get; set; }

        internal object State { get; private set; }

        public ulong TickCount { get; private set; }

        public Result Result { get; private set; }

        public void Tick(object state = null)
        {
            if (Root == null) { return; }

            CurrentInstance = this;
            State = state;

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
                Result = Result.Unknown;
                _rootCoroutine = Root.Execute();
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

        internal void OnNodeTickStarted(Node node)
        {
            _active.Push(node);
            OnNodeActiveEvent(node);
        }

        internal void OnNodeTickFinished()
        {
            var node = _active.Pop();
            OnNodeInactiveEvent(node);
        }

        internal void AddContinuation(IContinuation continuation)
        {
            _continuationsBuffer.Push(continuation);
        }

        private void ConsumeBuffers()
        {
            foreach (var continuation in _continuationsBuffer) { _continuations.Add(continuation); }
            _continuationsBuffer.Clear();

            // The tick finished for the whole tree.
            if (_continuations.Count == 0)
            {
                TickCount++;

                // Forces recycling, otherwise GetResult() is never called for the root coroutine.
                Result = _rootCoroutine.GetResult();
            }
        }

        protected virtual void OnNodeActiveEvent(Node node)
        {
            var handler = NodeActiveEventHandler;
            handler?.Invoke(this, node);
        }

        protected virtual void OnNodeInactiveEvent(Node node)
        {
            var handler = NodeInactiveEventHandler;
            handler?.Invoke(this, node);
        }
    }
}
