using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public abstract class Node
    {
        private readonly CoroutineManager _manager;

        protected Coroutine Yield => _manager.Yield;
        protected Coroutine<Result> Success => _manager.Success;
        protected Coroutine<Result> Failure => _manager.Failure;
        protected object State => _manager.State;

        protected Node(CoroutineManager manager)
        {
            _manager = manager;
        }

        public string Guid { get; set; }

        public async Coroutine<Result> Execute()
        {
            _manager.OnNodeTickStarted(this);

            Start();

            var result = await Tick();

            _manager.OnNodeTickFinished();

            return result;
        }

        protected virtual void Start() { }
        protected virtual void Stop() { }
        protected abstract Coroutine<Result> Tick();
    }
}
