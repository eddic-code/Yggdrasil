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
using System.Threading.Tasks;

namespace Yggdrasil.Coroutines
{
    // The real awaiter.
    public class CoroutineManager<T> : CoroutineManagerBase
    {
        private readonly Stack<CoroutineThread<T>> _iterationOpenSet = new Stack<CoroutineThread<T>>(100);
        private readonly Stack<CoroutineThread<T>> _iterationReverseSet = new Stack<CoroutineThread<T>>(100);
        private readonly List<CoroutineThread<T>> _threads = new List<CoroutineThread<T>>(100);

        internal readonly Coroutine Yield;
        private int _activeThreadIndex;
        private CoroutineThread<T> _mainThread;

        private Func<Coroutine<T>> _root;

        public CoroutineManager()
        {
            Yield = Coroutine.CreateConst(false);
        }

        public ulong TickCount => _mainThread?.TickCount ?? 0;

        public T Result => _mainThread.Result;

        public Func<Coroutine<T>> Root
        {
            get => _root;
            set
            {
                _root = value;
                _mainThread = new CoroutineThread<T>(_root, true, 0);
                Reset();
            }
        }

        public CoroutineThread<T> ActiveThread { get; private set; }

        public void Update()
        {
            if (Root == null) { return; }

            CurrentInstance = this;

            _activeThreadIndex = 0;
            ActiveThread = null;

            // Start a new tick.
            if (_threads.Count <= 0) { _threads.Add(_mainThread); }

            // Iterate over each thread. New threads might be added as we go along.
            // Only ticks a thread once per update.

            var dependenciesChanged = false;

            while (_threads.Count > _activeThreadIndex)
            {
                ActiveThread = _threads[_activeThreadIndex];
                ActiveThread.Tick();

                // If the thread is still running, leave it on the list.
                if (!ActiveThread.IsComplete)
                {
                    _activeThreadIndex += 1;
                    continue;
                }

                // Remove completed threads.
                _threads.RemoveAt(_activeThreadIndex);

                // Remove any inbound dependencies since they are now irrelevant.
                foreach (var d in ActiveThread.InputDependencies) { d.OutputDependencies.Remove(ActiveThread); }

                ActiveThread.InputDependencies.Clear();

                // Remove outbound dependencies and mark threads that can be ticked again this frame.
                foreach (var d in ActiveThread.OutputDependencies)
                {
                    d.InputDependencies.Remove(ActiveThread);
                    if (d.InputDependencies.Count <= 0)
                    {
                        d.DependenciesFinished = true;
                        dependenciesChanged = true;
                    }
                }
            }

            if (dependenciesChanged) { ProcessDependencies(); }

            CurrentInstance = null;
        }

        public void Reset()
        {
            foreach (var thread in _threads) { thread.Reset(); }

            _mainThread.Reset();
            _threads.Clear();
            _iterationOpenSet.Clear();
            _iterationReverseSet.Clear();

            ActiveThread = null;
            _activeThreadIndex = 0;
        }

        internal void ProcessThread(CoroutineThread<T> thread)
        {
            _threads.Add(thread);
        }

        internal void ProcessThreadAsDependency(CoroutineThread<T> thread)
        {
            _threads.Add(thread);
            thread.OutputDependencies.Add(ActiveThread);

            ActiveThread.InputDependencies.Add(thread);
            ActiveThread.DependenciesFinished = false;
        }

        internal void TerminateThreadAndDependencies(CoroutineThread<T> thread)
        {
            // All threads below this one must also be terminated.
            // We iterate inbound dependencies from bottom up.

            _iterationOpenSet.Clear();
            _iterationReverseSet.Clear();
            _iterationOpenSet.Push(thread);

            while (_iterationOpenSet.Count > 0)
            {
                var next = _iterationOpenSet.Pop();
                _iterationReverseSet.Push(next);

                foreach (var dependency in next.InputDependencies) { _iterationOpenSet.Push(dependency); }
            }

            while (_iterationReverseSet.Count > 0)
            {
                var t = _iterationReverseSet.Pop();

                t.Reset();
                if (!_threads.Contains(t)) { continue; }

                // The active thread is removed on the tick loop when not running anymore.
                if (ActiveThread == t) { continue; }

                var index = _threads.IndexOf(t);
                _threads.Remove(t);

                // Adjust the active thread index if necessary.
                if (index <= 0) { continue; }

                if (index < _activeThreadIndex) { _activeThreadIndex -= 1; }
            }
        }

        internal override void SetException(Exception exception)
        {
            Reset();

            // Temporary throw. Could log exception instead and continue.
            throw exception;
        }

        internal override void AddContinuation(IContinuation continuation)
        {
            ActiveThread.AddContinuation(continuation);
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
                    ActiveThread = _threads[_activeThreadIndex];
                    if (!ActiveThread.DependenciesFinished)
                    {
                        _activeThreadIndex += 1;
                        continue;
                    }

                    ActiveThread.DependenciesFinished = false;
                    ActiveThread.Tick();

                    // If the thread is still running, leave it on the list.
                    if (!ActiveThread.IsComplete)
                    {
                        _activeThreadIndex += 1;
                        continue;
                    }

                    // Remove completed threads.
                    _threads.RemoveAt(_activeThreadIndex);

                    // Remove any inbound dependencies since they are now irrelevant.
                    foreach (var d in ActiveThread.InputDependencies) { d.OutputDependencies.Remove(ActiveThread); }

                    ActiveThread.InputDependencies.Clear();

                    // Remove outbound dependencies and mark threads that can be ticked again this frame.
                    foreach (var d in ActiveThread.OutputDependencies)
                    {
                        d.InputDependencies.Remove(ActiveThread);
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
    }
}