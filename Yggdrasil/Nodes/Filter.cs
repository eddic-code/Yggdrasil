using System;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Filter : Node
    {
        public Filter(CoroutineManager manager, Func<object, bool> conditional) : base(manager)
        {
            Conditional = conditional;
        }

        public Filter(CoroutineManager manager) : base(manager)
        {
            
        }

        public Node Child { get; set; }

        public Func<object, bool> Conditional { get; set; } = DefaultConditional;

        protected override async Coroutine<Result> Tick()
        {
            if (Child == null) { return Result.Failure; }
            if (!Conditional(State)) { return Result.Failure; }

            return await Child.Execute();
        }

        private static bool DefaultConditional(object s)
        {
            return true;
        }
    }
}
