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
    public class NestedParallelNodeBenchmark
    {
        private CoroutineManager _manager;
        private State _state;

        [GlobalSetup]
        public void Setup()
        {
            _manager = new CoroutineManager();
            var parallelA = new Parallel(_manager);
            var parallelB = new Parallel(_manager);
            var parallelC = new Parallel(_manager);

            var root = new Sequence(_manager);
            var entryCondition = new Condition(_manager, s => ((State) s).Entry);

            var conditionalA = new TestYieldConditionNode(_manager) {Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode(_manager) {Conditional = s => s.B};
            var conditionalC = new TestYieldConditionNode(_manager) {Conditional = s => s.C};
            var conditionalD = new TestYieldConditionNode(_manager) {Conditional = s => s.D};
            var conditionalE = new TestRunningConditionNode(_manager) {Conditional = s => s.E};

            root.Children = new List<Node> {entryCondition, parallelA};

            parallelA.Children = new List<Node> {parallelB, parallelC, conditionalA};
            parallelB.Children = new List<Node> {conditionalB, conditionalC};
            parallelC.Children = new List<Node> {conditionalD, conditionalE};

            _manager.Root = root;

            _state = new State {Entry = true, A = true, B = true, C = true, D = true, E = true};

            while (_manager.TickCount == 0) { _manager.Update(_state); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Update(_state); }
        }

        private class State
        {
            public bool Entry;
            public bool A;
            public bool B;
            public bool C;
            public bool D;
            public bool E;
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

        private class TestRunningConditionNode : Node
        {
            public Func<State, bool> Conditional;

            public TestRunningConditionNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                await Yield;
                await Yield;
                await Yield;
                await Yield;
                await Yield;

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }
    }
}
