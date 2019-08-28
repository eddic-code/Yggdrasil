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
    public class ParallelNodeBenchmark
    {
        private CoroutineManager _manager;
        private State _state;

        [GlobalSetup]
        public void Setup()
        {
            _manager = new CoroutineManager();
            var root = new Parallel(_manager);

            var conditionalA = new TestYieldConditionNode(_manager) {Conditional = s => s.A};
            var conditionalB = new TestNestedConditionNode(_manager) {Conditional = s => s.B};
            var conditionalC = new TestConditionNode(_manager) {Conditional = s => s.C};
            var conditionalD = new TestRunningConditionNode(_manager) {Conditional = s => s.D};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC, conditionalD};
            _manager.Root = root;

            _state = new State {A = true, B = false, C = true, D = false};

            while (_manager.TickCount == 0) { _manager.Update(_state); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Update(_state); }
        }

        private class State
        {
            public bool A;
            public bool B;
            public bool C;
            public bool D;
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

        private class TestNestedConditionNode : Node
        {
            public Func<State, bool> Conditional;

            public TestNestedConditionNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                await MethodB();

                return await MethodC();
            }

            #pragma warning disable 1998
            private async Coroutine MethodB()
            #pragma warning restore 1998
            {
                
            }

            private async Coroutine<Result> MethodC()
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
