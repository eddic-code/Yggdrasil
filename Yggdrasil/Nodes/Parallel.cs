using System.Collections.Generic;
using System.Linq;
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
                    _threads.AddRange(value.Select(n => new CoroutineThread(n, false, 1)));
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

            while (_threads.Any(t => !t.IsComplete))
            {
                await Yield;
            }

            var result = _threads.Any(t => t.Result == Result.Success) 
                ? Result.Success 
                : Result.Failure;

            foreach (var thread in _threads) { thread.Reset(); }

            return result;
        }
    }
}
