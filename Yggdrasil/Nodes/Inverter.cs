using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Inverter : Node
    {
        public Inverter(CoroutineManager manager) : base(manager)
        {

        }

        public Node Child { get; set; }

        protected override async Coroutine<Result> Tick()
        {
            if (Child == null) { return Result.Failure; }

            var result = await Child.Execute();
            if (result == Result.Unknown) { return result; }

            return result == Result.Success ? Result.Failure : Result.Success;
        }
    }
}
