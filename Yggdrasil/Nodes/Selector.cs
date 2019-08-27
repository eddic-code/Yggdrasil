using System.Collections.Generic;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Selector : Node
    {
        public Selector(CoroutineManager manager) : base(manager)
        {

        }

        public List<Node> Children { get; set; }

        protected override async Coroutine<Result> Tick()
        {
            if (Children == null || Children.Count <= 0) { return Result.Failure; }

            foreach (var child in Children)
            {
                var result = await child.Execute();
                if (result == Result.Success) { return result; }
            }

            return Result.Failure;
        }
    }
}
