using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class CompositeTests
    {
        [TestMethod]
        public void SequenceNodeTest()
        {
            var manager = new CoroutineManager();
            var root = new Sequence(manager);
            var stages = new Queue<string>();

            manager.Root = root;

            var conditionalA = new TestConditionNode(manager) {Print="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode(manager) {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestConditionNode(manager) {Print="C", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};

            Assert.AreEqual(0UL, manager.TickCount);

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "A", "BYield"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State { A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State { A = true, B = false, C = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State { A = true, B = true, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(3UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State {A = false, B = true, C = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(4UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class State
        {
            public bool A;
            public bool B;
            public bool C;
        }

        private class TestConditionNode : Node
        {
            public string Print;

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

            public TestConditionNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                Stages.Enqueue(Print);

                return Conditional((State) State) ? Success : Failure;
            }
        }

        private class TestYieldConditionNode : Node
        {
            public string PrintA;
            public string PrintB;

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

            public TestYieldConditionNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue(PrintA);

                await Yield;

                Stages.Enqueue(PrintB);

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }
    }
}
