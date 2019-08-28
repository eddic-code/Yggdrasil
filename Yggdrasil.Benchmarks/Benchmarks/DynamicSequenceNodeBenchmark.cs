using System;
using System.Collections.Generic;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class DynamicSequenceNodeBenchmark
    {
        private CoroutineManager _manager;
        private object _state;

        [GlobalSetup]
        public void Setup()
        {
            _manager = new CoroutineManager();
            var root = new Sequence(_manager);

            var conditionalA = new TestConditionNode(_manager) {Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode(_manager) {Conditional = s => s.B};
            var conditionalC = new TestConditionNode(_manager) {Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            _manager.Root = root;

            dynamic state = new ExpandoObject();
            state.A = true;
            state.B = true;
            state.C = true;
            _state = state;

            while (_manager.TickCount == 0) { _manager.Update(_state); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Update(_state); }
        }

        private class TestConditionNode : Node
        {
            public Func<dynamic, bool> Conditional;

            public TestConditionNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                return Conditional(State) ? Success : Failure;
            }
        }

        private class TestYieldConditionNode : Node
        {
            public Func<dynamic, bool> Conditional;

            public TestYieldConditionNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                return Conditional(State) ? Result.Success : Result.Failure;
            }
        }
    }
}
