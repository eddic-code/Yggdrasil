namespace Yggdrasil
{
    public abstract class Node
    {
        private readonly CoroutineManager _manager;

        protected Coroutine Yield => _manager.Yield;

        public Node(CoroutineManager tree)
        {
            _manager = tree;
        }

        public virtual string Name { get; set; }

        public abstract Coroutine Tick();
    }
}
