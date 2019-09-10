using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class AsyncFlowTests
    {
        [TestMethod]
        public void ContinuationOrderTest()
        {
            var node = new TestNode();
            var stages = new Queue<string>();

            node.Stages = stages;

            var tree = new BehaviourTree(node);

            stages.Enqueue("TICK");
            tree.Update();

            var sequence = new List<string> { "TICK", "1A", "2A" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "2B", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2C: TRUE", "1B: 10"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "1C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class TestNode : Node
        {
            public Queue<string> Stages;

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue("1A");

                var result = await Method2();

                Stages.Enqueue($"1B: {result}");

                await Yield;

                Stages.Enqueue("1C");

                return Result.Success;
            }

            private async Coroutine<int> Method2()
            {
                Stages.Enqueue("2A");

                await Yield;

                Stages.Enqueue("2B");

                var result = await Method3();
                var txt = result ? "TRUE" : "FALSE";

                Stages.Enqueue($"2C: {txt}");

                return 10;
            }

            private async Coroutine<bool> Method3()
            {
                Stages.Enqueue("3A");

                await Yield;

                Stages.Enqueue("3B");

                return true;
            }
        }

        [TestMethod]
        public void NestedCoroutinesTest()
        {
            var node = new NestedCoroutinesTestNode();
            var stages = new Queue<string>();

            node.Stages = stages;

            var tree = new BehaviourTree(node);

            stages.Enqueue("TICK");
            tree.Update();

            var sequence = new List<string> { "TICK", "1A", "3A: 5" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B: 7", "1B", "2A"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "2B", "1C: 10", "3A: 10" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B: 12", "1D"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class NestedCoroutinesTestNode : Node
        {
            public Queue<string> Stages;

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue("1A");

                await Method3(5);

                Stages.Enqueue("1B");

                var result = await Method2();

                Stages.Enqueue($"1C: {result}");

                await Method3(result);

                Stages.Enqueue("1D");

                return Result.Success;
            }

            private async Coroutine<int> Method2()
            {
                Stages.Enqueue("2A");

                await Yield;

                Stages.Enqueue("2B");

                return 10;
            }

            private async Coroutine Method3(int value)
            {
                Stages.Enqueue($"3A: {value}");

                value += 1;

                await Yield;

                value += 1;

                Stages.Enqueue($"3B: {value}");
            }
        }

        [TestMethod]
        public void ContinuationLoopTest()
        {
            var node = new LoopTestNode();
            var stages = new Queue<string>();

            node.Stages = stages;

            var tree = new BehaviourTree(node);

            stages.Enqueue("TICK");
            tree.Update();

            var sequence = new List<string> { "TICK", "1A", "2A" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "2B", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: TRUE", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: FALSE", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: TRUE", "2C", "1Loop: 1", "2A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "2B", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: TRUE", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: FALSE", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new[] { "TICK", "3B", "2Loop: TRUE", "2C", "1Loop: 2", "1C" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            tree.Update();

            sequence.AddRange(new List<string> { "TICK", "1A", "2A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class LoopTestNode : Node
        {
            public Queue<string> Stages;

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue("1A");

                for (var i = 0; i < 2; i++)
                {
                    var result = await Method2(i);
                    Stages.Enqueue($"1Loop: {result}");
                }

                Stages.Enqueue("1C");

                return Result.Success;
            }

            private async Coroutine<int> Method2(int iteration)
            {
                Stages.Enqueue("2A");

                await Yield;

                Stages.Enqueue("2B");

                for (var i = 0; i < 3; i++)
                {
                    var result = await Method3(i);
                    var txt = result ? "TRUE" : "FALSE";
                    Stages.Enqueue($"2Loop: {txt}");
                }

                Stages.Enqueue($"2C");

                return iteration + 1;
            }

            private async Coroutine<bool> Method3(int iteration)
            {
                Stages.Enqueue("3A");

                await Yield;

                Stages.Enqueue("3B");

                return iteration != 1;
            }
        }
    }
}
