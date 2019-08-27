using System.Collections.Generic;
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
            manager.Root = root;

            var conditionalA = new Condition(manager, s => ((State)s).A);
            var conditionalB = new Condition(manager, s => ((State)s).B);

            root.Children = new List<Node> {conditionalA, conditionalB};

            manager.Tick(new State {A = true, B = true});

            Assert.AreEqual(Result.Success, manager.Result);

            manager.Tick(new State { A = true, B = false });

            Assert.AreEqual(Result.Failure, manager.Result);

            manager.Tick(new State { A = false, B = true });

            Assert.AreEqual(Result.Failure, manager.Result);
        }

        private class State
        {
            public bool A;
            public bool B;
        }

        private class SuccessNode : Node
        {
            public SuccessNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                return Success;
            }
        }

        private class FailureNode : Node
        {
            public FailureNode(CoroutineManager manager) : base(manager) { }

            protected override Coroutine<Result> Tick()
            {
                return Success;
            }
        }
    }
}
