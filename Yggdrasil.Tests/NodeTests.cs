using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Yggdrasil.Nodes;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class AsyncFlowTests
    {
        [TestMethod]
        public void ContinuationOrderTest()
        {
            var manager = new CoroutineManager();
            var node = new TestNode(manager);
            var stages = new Queue<string>();

            node.Stages = stages;
            manager.Root = node;

            stages.Enqueue("TICK");
            manager.Tick();

            var sequence = new List<string> { "TICK", "1A", "2A" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick();

            sequence.AddRange(new[] { "TICK", "2B", "3A" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick();

            sequence.AddRange(new[] { "TICK", "3B", "2C: TRUE", "1B: 10"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick();

            sequence.AddRange(new[] { "TICK", "1C"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class TestNode : Node
        {
            public Queue<string> Stages;

            public TestNode(CoroutineManager tree) : base(tree)
            {

            }

            public override async Coroutine Tick()
            {
                Stages.Enqueue("1A");

                var result = await Method2();

                Stages.Enqueue($"1B: {result}");

                await Yield;

                Stages.Enqueue("1C");
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
    }
}
