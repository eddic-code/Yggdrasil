using System;
using System.Xml.Serialization;
using Yggdrasil.Attributes;
using Yggdrasil.Behaviour;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

public class ExampleCustomAction : Node
{
    [XmlAttribute]
    public Result Output { get; set; }

    [XmlIgnore]
    [ScriptedFunction]
    public Action<object> Function { get; set; }

    [XmlAttribute]
    public int Yields { get; set; }

    protected override async Coroutine<Result> Tick()
    {
        for (var i = 0; i < Yields; i++)
        {
            Function?.Invoke(State);
            await Yield;
        }

        return Output;
    }
}
