#region License

// /*
// MIT License
// 
// Copyright (c) 2019 eddic-code
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// */

#endregion

using System.Collections.Generic;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Coroutines
{
    public class CoroutineThread
    {
        private readonly Stack<Node> _active = new Stack<Node>(100);
        private readonly List<IContinuation> _continuations = new List<IContinuation>(100);
        private readonly Stack<IContinuation> _continuationsBuffer = new Stack<IContinuation>(100);

        private Coroutine<Result> _rootCoroutine;

        public CoroutineThread(Node root, bool neverCompletes, ulong ticksToComplete)
        {
            Root = root;
            TicksToComplete = ticksToComplete;
            NeverCompletes = neverCompletes;
        }

        internal HashSet<CoroutineThread> OutputDependencies { get; } = new HashSet<CoroutineThread>();

        internal HashSet<CoroutineThread> InputDependencies { get; } = new HashSet<CoroutineThread>();

        public Node Root { get; }

        public ulong TicksToComplete { get; }

        public bool NeverCompletes { get; }

        public IEnumerable<Node> ActiveNodes => _active;

        internal bool DependenciesFinished { get; set; }

        public bool IsComplete { get; private set; }

        public Result Result { get; private set; }

        public bool IsRunning { get; private set; }

        public ulong TickCount { get; private set; }

        public void Tick()
        {
            if (Root == null) { return; }

            IsRunning = true;

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

            IsRunning = _continuations.Count > 0;
        }

        public void Reset()
        {
            foreach (var node in _active) { node.Terminate(); }

            IsRunning = false;
            TickCount = 0;
            IsComplete = false;
            Result = Result.Unknown;

            _active.Clear();

            foreach (var cont in _continuations) { cont.Discard(); }
            _continuations.Clear();

            foreach (var cont in _continuationsBuffer) { cont.Discard(); }
            _continuationsBuffer.Clear();

            InputDependencies.Clear();
            OutputDependencies.Clear();
            DependenciesFinished = false;
        }

        internal void OnNodeTickStarted(Node node)
        {
            _active.Push(node);
        }

        internal void OnNodeTickFinished()
        {
            _active.Pop();
        }

        internal void AddContinuation(IContinuation continuation)
        {
            _continuationsBuffer.Push(continuation);
        }

        private void ConsumeBuffers()
        {
            foreach (var continuation in _continuationsBuffer) { _continuations.Add(continuation); }

            _continuationsBuffer.Clear();

            // The tick finished for the whole subtree.
            if (_continuations.Count == 0)
            {
                // Forces recycling, otherwise GetResult() is never called for the root coroutine.
                Result = _rootCoroutine.GetResult();
                TickCount += 1;
                IsComplete = !NeverCompletes && TickCount >= TicksToComplete;
            }
        }
    }
}