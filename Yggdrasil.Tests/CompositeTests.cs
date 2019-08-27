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

            var conditionalA = new Condition(manager, s => ((State)s).A);
            var conditionalB = new Condition(manager, s => ((State)s).B);

            root.Children = new List<Node> {conditionalA, conditionalB};

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true});
            Assert.AreEqual(Result.Success, manager.Result);

            var sequence = new List<string> { "TICK" };
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State { A = true, B = false });
            Assert.AreEqual(Result.Failure, manager.Result);

            sequence.AddRange(new[] { "TICK" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            stages.Enqueue("TICK");
            manager.Tick(new State { A = false, B = true });
            Assert.AreEqual(Result.Failure, manager.Result);

            sequence.AddRange(new[] { "TICK" });
            Assert.IsTrue(stages.SequenceEqual(sequence));

            root.Children = new List<Node> {conditionalA, new SuccessNode(manager){Stages = stages}, conditionalB, new SuccessNode(manager){Stages = stages}};

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true});
            Assert.AreEqual(Result.Success, manager.Result);

            sequence.AddRange(new[] { "TICK", "Success", "Success"});
            Assert.IsTrue(stages.SequenceEqual(sequence));

            root.Children = new List<Node> {conditionalA, new FailureNode(manager){Stages = stages}, conditionalB, new SuccessNode(manager){Stages = stages}};

            stages.Enqueue("TICK");
            manager.Tick(new State {A = true, B = true});
            Assert.AreEqual(Result.Failure, manager.Result);

            sequence.AddRange(new[] { "TICK", "Failure"});
            Assert.IsTrue(stages.SequenceEqual(sequence));
        }

        private class State
        {
            public bool A;
            public bool B;
        }

        private class SuccessNode : Node
        {
            public Queue<string> Stages;

            public SuccessNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                Stages.Enqueue("Success");

                return Success;
            }
        }

        private class SuccessYieldNode : Node
        {
            public Queue<string> Stages;

            public SuccessYieldNode(CoroutineManager manager) : base(manager) { }

            protected override async Coroutine<Result> Tick()
            {
                Stages.Enqueue("Success Yield");

                await Yield;

                Stages.Enqueue("Success");

                return Result.Success;
            }
        }

        private class FailureNode : Node
        {
            public Queue<string> Stages;

            public FailureNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                Stages.Enqueue("Failure");

                return Failure;
            }
        }
    }
}
