using System;
using System.Collections.Generic;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class CoroutineManagerBenchmarks
    {
        private CoroutineManager _managerA, _managerB, _managerC, _managerD, _managerE, _managerF;
        private State _stateA, _stateB, _stateE;
        private object _dynamicStateA;

        [GlobalSetup]
        public void Setup()
        {
            SetupNestedParallel();
            SetupParallel();
            SetupGenericCoroutine();
            SetupNestedCoroutine();
            SetupSequence();
            SetupDynamicSequence();
        }

        public void SetupNestedParallel()
        {
            _managerA = new CoroutineManager();
            var parallelA = new Parallel();
            var parallelB = new Parallel();
            var parallelC = new Parallel();

            var root = new Sequence();
            var entryCondition = new Condition(s => ((State) s).Entry);

            var conditionalA = new TestYieldConditionNode {Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {Conditional = s => s.B};
            var conditionalC = new TestYieldConditionNode {Conditional = s => s.C};
            var conditionalD = new TestYieldConditionNode {Conditional = s => s.D};
            var conditionalE = new TestRunningConditionNode {Conditional = s => s.E};

            root.Children = new List<Node> {entryCondition, parallelA};

            parallelA.Children = new List<Node> {parallelB, parallelC, conditionalA};
            parallelB.Children = new List<Node> {conditionalB, conditionalC};
            parallelC.Children = new List<Node> {conditionalD, conditionalE};

            _managerA.Root = root;
            _stateA = new State {Entry = true, A = true, B = true, C = true, D = true, E = true};

            _managerA.Initialize();
            while (_managerA.TickCount == 0) { _managerA.Update(_stateA); }
        }

        public void SetupParallel()
        {
            _managerB = new CoroutineManager();
            var root = new Parallel();

            var conditionalA = new TestYieldConditionNode {Conditional = s => s.A};
            var conditionalB = new TestNestedConditionNode {Conditional = s => s.B};
            var conditionalC = new TestConditionNode {Conditional = s => s.C};
            var conditionalD = new TestRunningConditionNode {Conditional = s => s.D};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC, conditionalD};
            _managerB.Root = root;

            _stateB = new State {A = true, B = false, C = true, D = false};

            _managerB.Initialize();
            while (_managerB.TickCount == 0) { _managerB.Update(_stateB); }
        }

        public void SetupGenericCoroutine()
        {
            _managerC = new CoroutineManager();
            _managerC.Root = new GenericCoroutineTestNode();

            _managerC.Initialize();
            while (_managerC.TickCount == 0) { _managerC.Update(); }
        }

        public void SetupNestedCoroutine()
        {
            _managerD = new CoroutineManager();
            _managerD.Root = new NestedCoroutineTestNode();

            _managerD.Initialize();
            while (_managerD.TickCount == 0) { _managerD.Update(); }
        }

        public void SetupSequence()
        {
            _managerE = new CoroutineManager();
            var root = new Sequence();

            var conditionalA = new TestConditionNode {Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {Conditional = s => s.B};
            var conditionalC = new TestConditionNode {Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            _managerE.Root = root;

            _stateE = new State {A = true, B = true, C = true};

            _managerE.Initialize();
            while (_managerE.TickCount == 0) { _managerE.Update(_stateE); }
        }

        public void SetupDynamicSequence()
        {
            _managerF = new CoroutineManager();
            var root = new Sequence();

            var conditionalA = new TestDynamicConditionNode {Conditional = s => s.A};
            var conditionalB = new TestDynamicYieldConditionNode {Conditional = s => s.B};
            var conditionalC = new TestDynamicConditionNode {Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            _managerF.Root = root;

            dynamic state = new ExpandoObject();
            state.A = true;
            state.B = true;
            state.C = true;
            _dynamicStateA = state;

            _managerF.Initialize();
            while (_managerF.TickCount == 0) { _managerF.Update(_dynamicStateA); }
        }

        [Benchmark]
        public void BNestedParallel()
        {
            while (_managerA.TickCount == 1) { _managerA.Update(_stateA); }
        }

        [Benchmark]
        public void BParallel()
        {
            while (_managerB.TickCount == 1) { _managerB.Update(_stateB); }
        }

        [Benchmark]
        public void BGenericCoroutine()
        {
            while (_managerC.TickCount == 1) { _managerC.Update(); }
        }

        [Benchmark]
        public void BNestedCoroutine()
        {
            while (_managerD.TickCount == 1) { _managerD.Update(); }
        }

        [Benchmark]
        public void BSequence()
        {
            while (_managerE.TickCount == 1) { _managerE.Update(_stateE); }
        }

        [Benchmark]
        public void BDynamicSequence()
        {
            while (_managerF.TickCount == 1) { _managerF.Update(_dynamicStateA); }
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

            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }

        private class TestRunningConditionNode : Node
        {
            public Func<State, bool> Conditional;

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

        private class TestConditionNode : Node
        {
            public Func<State, bool> Conditional;

            protected override Coroutine<Result> Tick()
            {
                return Conditional((State) State) ? Success : Failure;
            }
        }

        private class TestNestedConditionNode : Node
        {
            public Func<State, bool> Conditional;

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

        private class GenericCoroutineTestNode : Node
        {
            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                var result = await MethodA();

                result = await MethodB(result);

                await MethodC(result == 50);

                await Yield;

                return Result.Success;
            }

            private async Coroutine<int> MethodA()
            {
                await Yield;

                var result = await MethodB(5);

                await Yield;

                return 10 * result;
            }

            private async Coroutine<int> MethodB(int input)
            {
                await Yield;

                return input;
            }

            private async Coroutine<bool> MethodC(bool input)
            {
                await Yield;

                return !input;
            }
        }

        private class NestedCoroutineTestNode : Node
        {
            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                await MethodA();

                await Yield;

                await MethodB();
                await MethodC();

                await Yield;

                await MethodD();
                await MethodA();

                await Yield;

                return Result.Success;
            }

            private async Coroutine MethodA()
            {
                await Yield;

                await MethodB();

                await Yield;
            }

            private async Coroutine MethodB()
            {
                await Yield;
            }

            private async Coroutine MethodC()
            {
                await Yield;
            }

            // No awaits inside for testing purposes.
#pragma warning disable 1998
            private async Coroutine MethodD()
#pragma warning restore 1998
            {

            }
        }

        private class TestDynamicConditionNode : Node
        {
            public Func<dynamic, bool> Conditional;

            protected override Coroutine<Result> Tick()
            {
                return Conditional(State) ? Success : Failure;
            }
        }

        private class TestDynamicYieldConditionNode : Node
        {
            public Func<dynamic, bool> Conditional;

            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                return Conditional(State) ? Result.Success : Result.Failure;
            }
        }
    }
}
