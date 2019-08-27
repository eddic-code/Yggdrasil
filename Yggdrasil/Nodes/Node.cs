using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public abstract class Node
    {
        protected readonly CoroutineManager Manager;

        protected Coroutine Yield => Manager.Yield;
        protected Coroutine<Result> Success => Coroutine<Result>.CreateWith(Result.Success);
        protected Coroutine<Result> Failure => Coroutine<Result>.CreateWith(Result.Failure);
        protected object State => Manager.State;

        protected Node(CoroutineManager manager)
        {
            Manager = manager;
        }

        public string Guid { get; set; }

        public async Coroutine<Result> Execute()
        {
            Manager.OnNodeTickStarted(this);

            Start();

            var result = await Tick();

            Stop();

            Manager.OnNodeTickFinished(this);

            return result;
        }

        public virtual void Terminate() { }

        protected virtual void Start() { }
        protected virtual void Stop() { }
        protected abstract Coroutine<Result> Tick();
    }
}
