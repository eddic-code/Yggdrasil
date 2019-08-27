using System.Collections.Generic;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Coroutines
{
    public class CoroutineThread
    {
        private readonly Stack<IContinuation> _continuationsBuffer = new Stack<IContinuation>(100);
        private readonly List<IContinuation> _continuations = new List<IContinuation>(100);

        private Coroutine<Result> _rootCoroutine;

        public Node Root { get; set; }

        public Result Result { get; private set; }

        public bool IsRunning { get; private set; }

        // Returns true whenever a full subtree tick was completed.
        public void Tick()
        {
            IsRunning = false;
            if (Root == null) { return; }

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
            IsRunning = false;

            Result = Result.Unknown;

            _continuations.Clear();
            _continuationsBuffer.Clear();
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
            }
        }
    }
}
