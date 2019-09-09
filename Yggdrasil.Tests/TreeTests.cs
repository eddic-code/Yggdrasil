using System;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;
using Yggdrasil.Scripting;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class TreeTests
    {
        [TestMethod]
        [DeploymentItem("ParserTests\\testScriptB.ygg")]
        public void TestScriptB()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var manager = new CoroutineManager();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\testScriptB.ygg");

            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(3, context.TopmostNodeCount);

            var root = context.Instantiate("root", manager);
            Assert.IsNotNull(root);

            var state = new TestState();
            manager.Root = root;

            // Tick 1
            manager.Update(state);

            Assert.AreEqual(1, state.A);
            Assert.AreEqual(0, state.B);
            Assert.AreEqual(2, state.C);
            Assert.AreEqual(0, state.D);

            // Tick 2
            manager.Update(state);

            Assert.AreEqual(2, state.A);
            Assert.AreEqual(0, state.B);
            Assert.AreEqual(3, state.C);
            Assert.AreEqual(0, state.D);

            // Tick 3
            manager.Update(state);

            Assert.AreEqual(3, state.A);
            Assert.AreEqual(0, state.B);
            Assert.AreEqual(4, state.C);
            Assert.AreEqual(0, state.D);

            // Tick 4
            manager.Update(state);

            Assert.AreEqual(3, state.A);
            Assert.AreEqual(1, state.B);
            Assert.AreEqual(103, state.C);
            Assert.AreEqual(3, state.D);
            Assert.IsFalse(state.EventHappened);

            // Tick 5
            manager.Update(state);

            Assert.AreEqual(3, state.A);
            Assert.AreEqual(1, state.B);
            Assert.AreEqual(103, state.C);
            Assert.AreEqual(3, state.D);
            Assert.IsTrue(state.EventHappened);

            // Tick 6
            manager.Update(state);

            Assert.AreEqual(4, state.A);
            Assert.AreEqual(1, state.B);
            Assert.AreEqual(103, state.C);
            Assert.AreEqual(3, state.D);
            Assert.IsTrue(state.EventHappened);
        }

        public class TestState
        {
            public bool EventHappened;
            public int A, B, C, D, E;
            public string FirstName, SecondName, ThirdName;
        }

        public class TestRunningAction : Node
        {
            [XmlAttribute]
            public Result Output { get; set; }

            [XmlIgnore]
            [ScriptedFunction]
            public Action<object> Function { get; set; }

            [XmlAttribute]
            public int Yields { get; set; }

            protected override async Coroutine<Result> Tick()
            {
                for (var i = 0; i < Yields; i++)
                {
                    Function?.Invoke(State);
                    await Yield;
                }

                return Output;
            }
        }
    }
}
