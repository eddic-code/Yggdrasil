namespace Yggdrasil.Nodes
{
    public class Decorator : Node
    {
        public Decorator(CoroutineManager tree) : base(tree)
        {
            
        }

        public override async Coroutine Tick()
        {
            await Yield;
        }
    }
}
