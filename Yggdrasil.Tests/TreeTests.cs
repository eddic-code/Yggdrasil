using System;
using System.Dynamic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;
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
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\testScriptB.ygg");

            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(3, context.TopmostNodeCount);

            var root = context.Instantiate("root");
            Assert.IsNotNull(root);

            var manager = new BehaviourTree(root);
            AssertScriptB(manager);

            manager.Reset();
            AssertScriptB(manager);
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\testScriptB.ygg")]
        public void RepeatedParsingTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);

            for (var i = 0; i < 5; i++)
            {
                var context = parser.BuildFromFiles<TestState>("ParserTests\\testScriptB.ygg");

                Assert.AreEqual(0, context.Errors.Count);
                Assert.AreEqual(3, context.TopmostNodeCount);

                var root = context.Instantiate("root");
                Assert.IsNotNull(root);

                var manager = new BehaviourTree(root);
                AssertScriptB(manager);
            }
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\repeatedGuidTest.ygg")]
        public void RepeatedGuidsTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\repeatedGuidTest.ygg");

            Assert.IsTrue(context.Errors.Count > 0);
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\repeatedTypeDefTest.ygg")]
        public void RepeatedTypeDefTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\repeatedTypeDefTest.ygg");

            Assert.IsTrue(context.Errors.Count > 0);
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\malformedCSharpTest.ygg")]
        public void MalformedCSharpTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\malformedCSharpTest.ygg");

            Assert.IsTrue(context.Errors.Count > 0);
            Assert.IsTrue(context.Errors.Any(e => e.Diagnostics.Count > 0));
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\malformedXmlTest.ygg")]
        public void MalformedXmlTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\malformedXmlTest.ygg");

            Assert.IsTrue(context.Errors.Count > 0);
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\testScriptC.ygg")]
        public void DynamicTestScript()
        {
            var config = new YggParserConfig();

            config.ReplaceObjectStateWithDynamic = true;
            config.ScriptNamespaces.Add(typeof(Result).Namespace);
            config.ReferenceAssemblyPaths.Add(typeof(Node).Assembly.Location);
            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestDynamicFunction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<object>("ParserTests\\testScriptC.ygg");

            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(1, context.TopmostNodeCount);

            var root = context.Instantiate("root");
            Assert.IsNotNull(root);

            dynamic state = new ExpandoObject();

            var manager = new BehaviourTree(root);
            manager.Update(state);

            Assert.AreEqual(1UL, manager.TickCount);
            Assert.AreEqual(Result.Success, manager.Result);
            Assert.AreEqual(1, state.A);
        }

        private static void AssertScriptB(BehaviourTree manager)
        {
            var state = new TestState();

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
            Assert.AreEqual(1UL, manager.TickCount);
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

        public class DynamicLeaf : Node
        {
            [XmlAttribute]
            public Result Output { get; set; }

            [XmlIgnore]
            [ScriptedFunction]
            public Action<dynamic> Function { get; set; }

            protected override Coroutine<Result> Tick()
            {
                Function?.Invoke(State);

                return Output == Result.Success ? Success : Failure;
            }
        }

        public class TestDynamicFunction : Node
        {
            [XmlIgnore]
            [ScriptedFunction]
            public Func<dynamic, dynamic> Function { get; set; }

            protected override Coroutine<Result> Tick()
            {
                var output = Function?.Invoke(State);

                return output == Result.Success ? Success : Failure;
            }
        }
    }
}
