using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Behaviour;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Parallel = Yggdrasil.Behaviour.Parallel;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class ExternalTaskTests
    {
        [TestMethod]
        public void AsynchronousTaskTest()
        {
            var stages = new Queue<string>();

            var parallelA = new Parallel();
            var parallelB = new Parallel();
            var parallelC = new Parallel();

            var root = new Sequence();
            var entryCondition = new Condition(s => ((State) s).Entry);

            var conditionalA = new TestYieldConditionNode {PrintA="AYield", PrintB="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestYieldConditionNode {PrintA="CYield", PrintB="C", Stages = stages, Conditional = s => s.C};
            var conditionalD = new TestYieldConditionNode {PrintA="DYield", PrintB="D", Stages = stages, Conditional = s => s.D};
            var conditionalE = new TestRunningConditionNode {PrintA="E", Stages = stages, Conditional = s => s.E};

            parallelA.Children = new List<Node> {parallelB, parallelC, conditionalA};
            parallelB.Children = new List<Node> {conditionalB, conditionalC};
            parallelC.Children = new List<Node> {conditionalD, conditionalE};

            root.Children = new List<Node> {entryCondition, parallelA};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            var manager = new BehaviourTree(root);
            var state = new State {Entry = true, A = true, B = true, C = true, D = true, E = true};

            for (var i = 0; i < 2; i++)
            {
                var sequence = new[] {"AYield", "BYield", "CYield", "DYield"};
                var initialTick = manager.TickCount;

                while (initialTick == manager.TickCount) { manager.Update(state); }

                Assert.AreEqual(Result.Success, manager.Result);
                Assert.AreEqual(initialTick + 1UL, manager.TickCount);
                Assert.IsTrue(stages.Take(4).SequenceEqual(sequence));
                Assert.IsTrue(stages.Any(s => s == "A"));
                Assert.IsTrue(stages.Any(s => s == "B"));
                Assert.IsTrue(stages.Any(s => s == "C"));
                Assert.IsTrue(stages.Any(s => s == "D"));

                stages.Clear();
            }
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
            public string PrintA;
            public string PrintB;

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue(PrintA);

                await Yield;

                var print = await RunAsync(Task.Run(async () =>
                {
                    await Task.Delay(100);
                    return PrintB;
                }));

                Stages.Enqueue(print);

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }

        private class TestRunningConditionNode : Node
        {
            public string PrintA;

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

            protected override async Coroutine<Result> Tick()
            {
                await Yield;
                await Yield;
                await Yield;
                await MethodA();

                Stages.Enqueue(PrintA);

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }

            private async Coroutine MethodA()
            {
                await Yield;
                await MethodB();
            }

            private async Coroutine MethodB()
            {
                await Yield;
            }
        }
    }
}
