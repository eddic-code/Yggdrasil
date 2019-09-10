using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Behaviour;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class CoroutinePoolingTests
    {
        [TestMethod]
        public void CorouinePoolLeakTest()
        {
            var stages = new Queue<string>();
            var sequence = new List<string>();

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

            Coroutine<Result>.Pool.Clear();
            Coroutine.Pool.Clear();

            TickOnce(manager, stages, sequence);

            var genericPoolCount = Coroutine<Result>.Pool.Count;
            var voidPoolCount = Coroutine.Pool.Count;

            for (var i = 0; i < 100; i++)
            {
                TickOnce(manager, stages, sequence);

                Assert.IsTrue(Coroutine<Result>.Pool.Count > 0);
                Assert.IsTrue(Coroutine.Pool.Count > 0);
                Assert.AreEqual(genericPoolCount, Coroutine<Result>.Pool.Count);
                Assert.AreEqual(voidPoolCount, Coroutine.Pool.Count);
            }
        }

        private static void TickOnce(BehaviourTree manager, Queue<string> stages, List<string> sequence)
        {
            var initialTick = manager.TickCount;

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK", "AYield", "BYield", "CYield", "DYield"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK", "A", "B", "C", "D"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            sequence.AddRange(new[] {"TICK", "E"});
            manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.IsTrue(stages.SequenceEqual(sequence));
            Assert.AreEqual(initialTick + 1UL, manager.TickCount);

            stages.Clear();
            sequence.Clear();
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

                Stages.Enqueue(PrintB);

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
