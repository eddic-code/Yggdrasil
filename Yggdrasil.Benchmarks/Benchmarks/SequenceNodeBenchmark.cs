using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class SequenceNodeBenchmark
    {
        private CoroutineManager _manager;
        private State _state;

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

            _state = new State {A = true, B = true, C = true};

            while (_manager.TickCount == 0) { _manager.Tick(_state); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Tick(_state); }
        }

        private class State
        {
            public bool A;
            public bool B;
            public bool C;
        }

        private class TestConditionNode : Node
        {
            public Func<State, bool> Conditional;

            public TestConditionNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                return Conditional((State) State) ? Success : Failure;
            }
        }

        private class TestYieldConditionNode : Node
        {
            public Func<State, bool> Conditional;

            public TestYieldConditionNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }
    }
}
