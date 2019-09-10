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

        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>(100);

        internal readonly Coroutine Yield;
        private CoroutineThread _activeThread;
        private int _activeThreadIndex;
        private bool _initialized;
        private CoroutineThread _mainThread;

        private Node _root;

        public CoroutineManager()
        {
            Yield = Coroutine.CreateConst(false);
        }

        public ulong TickCount => _mainThread?.TickCount ?? 0;

        public Result Result => _mainThread?.Result ?? Result.Unknown;

        public Node Root
        {
            get => _root;
            set
            {
                _root = value;
                _initialized = false;
                _mainThread = new CoroutineThread(value, true, 0);
                Reset();
            }
        }

        internal object State { get; private set; }

        public event EventHandler<Node> NodeActiveEventHandler;

        public event EventHandler<Node> NodeInactiveEventHandler;

        public void Initialize()
        {
            var open = new Stack<Node>();
            open.Push(Root);

            while (open.Count > 0)
            {
                var next = open.Pop();
                next.Initialize();

                if (next.Children == null) { continue; }

                foreach (var c in next.Children) { open.Push(c); }
            }

            _initialized = true;
        }

        public void Update(object state = null)
        {
            if (Root == null) { return; }

            if (!_initialized) { Initialize(); }

            CurrentInstance = this;
            State = state;

            _activeThreadIndex = 0;
            _activeThread = null;

            // Start a new tick.
            if (_threads.Count <= 0) { _threads.Add(_mainThread); }

            // Iterate over each thread. New threads might be added as we go along.
            // Only ticks a thread once per update.

            var dependenciesChanged = false;

            while (_threads.Count > _activeThreadIndex)
            {
                _activeThread = _threads[_activeThreadIndex];
                _activeThread.Tick();

                // If the thread is still running, leave it on the list.
                if (!_activeThread.IsComplete)
                {
                    _activeThreadIndex += 1;
                    continue;
                }

                // Remove completed threads.
                _threads.RemoveAt(_activeThreadIndex);

                // Remove any inbound dependencies since they are now irrelevant.
                foreach (var d in _activeThread.InputDependencies) { d.OutputDependencies.Remove(_activeThread); }

                _activeThread.InputDependencies.Clear();

                // Remove outbound dependencies and mark threads that can be ticked again this frame.
                foreach (var d in _activeThread.OutputDependencies)
                {
                    d.InputDependencies.Remove(_activeThread);
                    if (d.InputDependencies.Count <= 0)
                    {
                        d.DependenciesFinished = true;
                        dependenciesChanged = true;
                    }
                }
            }

            if (dependenciesChanged) { ProcessDependencies(); }
        }

        public void Reset()
        {
            foreach (var thread in _threads) { thread.Reset(); }

            _threads.Clear();
            _mainThread.Reset();
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
            _activeThread.OnNodeTickStarted(node);
            OnNodeActiveEvent(node);
        }

        internal void OnNodeTickFinished(Node node)
        {
            _activeThread.OnNodeTickFinished();
            OnNodeInactiveEvent(node);
        }

        internal void ProcessThread(CoroutineThread thread)
        {
            _threads.Add(thread);
        }

        internal void ProcessThreadAsDependency(CoroutineThread thread)
        {
            _threads.Add(thread);
            thread.OutputDependencies.Add(_activeThread);

            _activeThread.InputDependencies.Add(thread);
            _activeThread.DependenciesFinished = false;
        }

        internal void TerminateThread(CoroutineThread thread)
        {
            thread.Reset();
            if (!_threads.Contains(thread)) { return; }

            // The active thread is removed on the tick loop when not running anymore.
            if (_activeThread == thread) { return; }

            var index = _threads.IndexOf(thread);
            _threads.Remove(thread);

            // Adjust the active thread index if necessary.
            if (index < _activeThreadIndex) { _activeThreadIndex -= 1; }
        }

        internal void AddContinuation(IContinuation continuation)
        {
            _activeThread.AddContinuation(continuation);
        }

        private void ProcessDependencies()
        {
            bool dependenciesFinished;

            // Iterate over each thread. New threads might be added as we go along.
            // Only ticks a thread once per update.
            do
            {
                dependenciesFinished = false;
                _activeThreadIndex = 0;

                while (_threads.Count > _activeThreadIndex)
                {
                    _activeThread = _threads[_activeThreadIndex];
                    if (!_activeThread.DependenciesFinished)
                    {
                        _activeThreadIndex += 1;
                        continue;
                    }

                    _activeThread.DependenciesFinished = false;
                    _activeThread.Tick();

                    // If the thread is still running, leave it on the list.
                    if (!_activeThread.IsComplete)
                    {
                        _activeThreadIndex += 1;
                        continue;
                    }

                    // Remove completed threads.
                    _threads.RemoveAt(_activeThreadIndex);

                    // Remove any inbound dependencies since they are now irrelevant.
                    foreach (var d in _activeThread.InputDependencies) { d.OutputDependencies.Remove(_activeThread); }

                    _activeThread.InputDependencies.Clear();

                    // Remove outbound dependencies and mark threads that can be ticked again this frame.
                    foreach (var d in _activeThread.OutputDependencies)
                    {
                        d.InputDependencies.Remove(_activeThread);
                        if (d.InputDependencies.Count <= 0)
                        {
                            d.DependenciesFinished = true;
                            dependenciesFinished = true;
                        }
                    }
                }
            }
            while (dependenciesFinished);
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