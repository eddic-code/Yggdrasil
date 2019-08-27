using System;
using System.Collections.Generic;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Coroutines
{
    // The real awaiter.
    public class CoroutineManager
    {
        [ThreadStatic]
        internal static CoroutineManager CurrentInstance;

        private readonly Stack<Node> _active = new Stack<Node>(100);
        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>(100);

        private Node _root;
        private CoroutineThread _mainThread;
        private CoroutineThread _activeThread;
        private int _activeThreadIndex;

        internal readonly Coroutine Yield;

        public event EventHandler<Node> NodeActiveEventHandler;

        public event EventHandler<Node> NodeInactiveEventHandler;

        public CoroutineManager()
        {
            Yield = new Coroutine();
        }

        public Node Root
        {
            get => _root;
            set
            {
                _root = value;
                _mainThread = CreateThread(value);
                Reset();
            }
        }

        internal object State { get; private set; }

        public ulong TickCount { get; private set; }

        public Result Result { get; private set; }

        public void Update(object state = null)
        {
            if (Root == null) { return; }

            Result = Result.Unknown;
            CurrentInstance = this;
            State = state;

            _activeThreadIndex = 0;

            // Start a new tick.
            if (_threads.Count <= 0) { _threads.Add(_mainThread); }

                // Iterate over each thread. New threads might be added as we go along.
            // Only ticks a thread once per update.
            while (_threads.Count > _activeThreadIndex)
            {
                _activeThread = _threads[_activeThreadIndex];
                _activeThread.Tick();

                // If the thread is still running, leave it on the list.
                if (_activeThread.IsRunning)
                {
                    _activeThreadIndex += 1;
                    continue;
                }

                // Remove completed threads.
                _threads.RemoveAt(_activeThreadIndex);
            }

            // Full tick of the tree.
            if (!_mainThread.IsRunning)
            {
                TickCount += 1;
                Result = _mainThread.Result;
            }
        }

        public void Reset()
        {
            TickCount = 0;
            Result = Result.Unknown;

            _active.Clear();
            _threads.Clear();

            _activeThread = null;
            _activeThreadIndex = 0;
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
            _activeThread.AddContinuation(continuation);
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

        private static CoroutineThread CreateThread(Node root)
        {
            return new CoroutineThread
            {
                Root = root
            };
        }
    }
}
