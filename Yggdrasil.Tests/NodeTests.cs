using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void SequenceNodeTest()
        {
            var root = new Sequence();
            var stages = new Queue<string>();

            var conditionalA = new TestConditionNode {Print="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestConditionNode {Print="C", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            var manager = new BehaviourTree(root);

            Assert.AreEqual(0UL, manager.TickCount);

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "A", "BYield"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = false, C = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = true, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(3UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = true, C = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(4UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void ConditionNodeTest()
        {
            var root = new Sequence();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            var conditionalA = new TestConditionNode {Print="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new Condition(s => ((State) s).B);
            var conditionalC = new TestConditionNode {Print="C", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "A", "C"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = false, C = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void SelectorNodeTest()
        {
            var root = new Selector();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            var conditionalA = new TestConditionNode {Print="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestConditionNode {Print="C", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // false false false
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = false, C = false});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "A", "BYield"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = false, B = false, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // false false true
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = false, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = false, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // false true true
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A", "BYield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(3UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // true true true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(4UL, manager.TickCount);

            sequence.AddRange(new[] {  "TICK", "A"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void InverterNodeTest()
        {
            var root = new Inverter();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            root.Children.Add(new TestYieldConditionNode() {PrintA="Yield", PrintB="A", Stages = stages, Conditional = s => s.A});
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // true => false
            stages.Enqueue("TICK");
            manager.Update(new State {A = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "Yield" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // false => true
            stages.Enqueue("TICK");
            manager.Update(new State { A = false });

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "Yield" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = false });

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void FilterNodeTest()
        {
            var root = new Filter();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            root.Conditional = s => ((State) s).A;
            root.Children.Add(new TestYieldConditionNode {PrintA="Yield", PrintB="B", Stages = stages, Conditional = s => s.B});
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // true true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "Yield" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "B"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // false true
            stages.Enqueue("TICK");
            manager.Update(new State { A = false, B = true});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK" });
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void NestedCoroutinesNodeTest()
        {
            var root = new Sequence();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            var conditionalA = new TestConditionNode {Print="A0", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestNestedConditionNode {Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestConditionNode {Print="C0", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // true true true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "A0", "A", "B", "C"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State { A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "D", "C0" });
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void ParallelNodeTest()
        {
            var root = new Parallel();
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            var conditionalA = new TestYieldConditionNode {PrintA="AYield", PrintB="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            var conditionalC = new TestYieldConditionNode {PrintA="CYield", PrintB="C", Stages = stages, Conditional = s => s.C};

            root.Children = new List<Node> {conditionalA, conditionalB, conditionalC};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // true true true => true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "AYield", "BYield", "CYield"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = true, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // true false true => true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = false, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] {"TICK", "AYield", "BYield", "CYield"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = false, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // false false false => false
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = false, C = false});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {"TICK", "AYield", "BYield", "CYield"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = false, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(3UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A", "B", "C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void ParallelFilterNodeTest()
        {
            var root = new Interrupt(s => ((State)s).C);
            var stages = new Queue<string>();
            var manager = new BehaviourTree(root);

            var conditionalA = new TestYieldConditionNode {PrintA="AYield", PrintB="A", Stages = stages, Conditional = s => s.A};
            var conditionalB = new TestYieldConditionNode {PrintA="BYield", PrintB="B", Stages = stages, Conditional = s => s.B};
            
            root.Children = new List<Node> {conditionalA, conditionalB};
            foreach (var n in root.DepthFirstIterate()) { n.Initialize(); }

            Assert.AreEqual(0UL, manager.TickCount);

            // x x true => true
            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = false, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(0UL, manager.TickCount);

            var sequence = new List<string> { "TICK", "AYield", "BYield"};
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = true, B = false, C = true});

            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1UL, manager.TickCount);

            sequence.AddRange(new[] { "TICK", "A", "B"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // x x false => false
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = true, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {"TICK"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            // x x delayed false => false
            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = true, C = true});

            Assert.AreEqual(Result.Unknown, manager.Result);
            Assert.AreEqual(2UL, manager.TickCount);

            sequence.AddRange(new[] {"TICK", "AYield", "BYield"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Update(new State {A = false, B = true, C = false});

            Assert.AreEqual(Result.Failure, manager.Result);
            Assert.AreEqual(3UL, manager.TickCount);

            sequence.AddRange(new[] {"TICK"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        [TestMethod]
        public void NestedParallelNodeTest()
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
            var sequence = new List<string>();

            for (var i = 0; i < 2; i++)
            {
                Assert.AreEqual(0UL, manager.TickCount);

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK", "AYield", "BYield", "CYield", "DYield"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Unknown, manager.Result);
                Assert.AreEqual(0UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK", "A", "B", "C", "D"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Unknown, manager.Result);
                Assert.AreEqual(0UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Unknown, manager.Result);
                Assert.AreEqual(0UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Unknown, manager.Result);
                Assert.AreEqual(0UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Unknown, manager.Result);
                Assert.AreEqual(0UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                stages.Enqueue("TICK");
                sequence.AddRange(new[] {"TICK", "E"});
                manager.Update(new State {Entry = true, A = true, B = true, C = true, D = true, E = true});

                Assert.AreEqual(Result.Success, manager.Result);
                Assert.AreEqual(1UL, manager.TickCount);
                Assert.IsTrue(stages.SequenceEqual(sequence));

                manager.Reset();
                stages.Clear();
                sequence.Clear();
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

        private class TestConditionNode : Node
        {
            public string Print;

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

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

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue(PrintA);

                await Yield;

                Stages.Enqueue(PrintB);

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }

        private class TestNestedConditionNode : Node
        {
            public string PrintA = "A";
            public string PrintB = "B";
            public string PrintC = "C";
            public string PrintD = "D";

            public Queue<string> Stages;

            public Func<State, bool> Conditional;

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue(PrintA);

                await MethodB();

                return await MethodC();
            }

            #pragma warning disable 1998
            private async Coroutine MethodB()
            #pragma warning restore 1998
            {
                Stages.Enqueue(PrintB);
            }

            private async Coroutine<Result> MethodC()
            {
                Stages.Enqueue(PrintC);

                await Yield;

                Stages.Enqueue(PrintD);

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
                await Yield;
                await Yield;

                Stages.Enqueue(PrintA);

                return Conditional((State) State) ? Result.Success : Result.Failure;
            }
        }
    }
}
