using System.Collections.Generic;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Parallel : Node
    {
        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>(10);
        private List<Node> _children;

        public Parallel(CoroutineManager manager) : base(manager) { }

        public List<Node> Children
        {
            get => _children;
            set
            {
                _children = value;
                _threads.Clear();

                if (value != null && value.Count > 0)
                {
                    foreach (var n in value)
                    {
                        var thread = new CoroutineThread(n, false, 1);
                        _threads.Add(thread);
                    }
                }
            }
        }

        public override void Terminate()
        {
            foreach (var thread in _threads) { thread.Reset(); }
        }

        protected override async Coroutine<Result> Tick()
        {
            foreach (var thread in _threads) 
            { 
                Manager.ProcessThreadAsDependency(thread);
            }

            while (Continue())
            {
                await Yield;
            }

            var result = Result.Failure;

            foreach (var thread in _threads)
            {
                if (thread.Result == Result.Success) { result = Result.Success; }
                thread.Reset();
            }

            return result;
        }

        private bool Continue()
        {
            var processing = false;
            foreach (var thread in _threads) { processing = processing || !thread.IsComplete; }
            return processing;
        }
    }
}
