using System;
using System.Xml.Serialization;
using Yggdrasil.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Leaf : Node
    {
        [XmlAttribute]
        public Result Output { get; set; }

        [XmlIgnore]
        [ScriptedFunction]
        public Action<object> Function { get; set; }

        protected override Coroutine<Result> Tick()
        {
            Function?.Invoke(State);

            return Output == Result.Success ? Success : Failure;
        }
    }
}
