using System;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Condition : Node
    {
        public Condition(CoroutineManager manager, Func<object, bool> conditional) : base(manager)
        {
            Conditional = conditional;
        }

        public Condition(CoroutineManager manager) : base(manager)
        {
            
        }

        public Func<object, bool> Conditional { get; set; } = DefaultConditional;

        protected override async Coroutine<Result> Tick()
        {
            return Conditional(State) ? Result.Success : Result.Failure;
        }

        private static bool DefaultConditional(object s)
        {
            return true;
        }
    }
}
