using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;
using Yggdrasil.Scripting;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        [DeploymentItem("ParserTests\\escapeCharacterTest.ygg")]
        public void EscapeCharacterTest()
        {
            var parser = new YggParser(null, new YggCompiler());
            var document = parser.LoadFromFile("ParserTests\\escapeCharacterTest.ygg");
            
            const string innerTextA = @"state.A >= state.B || state.C <= state.D";
            Assert.AreEqual(innerTextA, document.SelectSingleNode("/__Main/FilterA/Conditional").InnerText);

            const string attributeTextB = @"state.A >= state.B && state.C <= state.D";
            Assert.AreEqual(attributeTextB, document.SelectSingleNode("/__Main/FilterB").Attributes[0].Value);

            const string attributeTextC = "state.A == \"hello\"";
            Assert.AreEqual(attributeTextC, document.SelectSingleNode("/__Main/FilterC").Attributes[0].Value);

            const string attributeTextD = "state.A == \"hello\" && state.C <= state.N";
            Assert.AreEqual(attributeTextD, document.SelectSingleNode("/__Main/FilterD").Attributes[0].Value);

            const string attributeTextA0 = "state.A == \"hello\" && state.C <= state.N || state.D > 10";
            Assert.AreEqual(attributeTextA0, document.SelectSingleNode("/__Main/FilterD/NodeA").Attributes[0].Value);
            Assert.AreEqual(attributeTextA0, document.SelectSingleNode("/__Main/FilterD/NodeA").Attributes[1].Value);

            const string attributeTextB0 = "state.A >= state.B || state.C <= state.D";
            const string attributeTextB1 = "state.A > state.B || state.C < state.D";

            Assert.AreEqual(attributeTextB0, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeB").Attributes[0].Value);
            Assert.AreEqual(attributeTextB1, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeB").InnerText);

            Assert.AreEqual(attributeTextB1, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeC").Attributes[0].Value);
            Assert.AreEqual(attributeTextB0, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeC").InnerText);

            Assert.AreEqual(attributeTextB1, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeD").Attributes[0].Value);
            Assert.AreEqual(attributeTextB0, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeD").InnerText);

            const string innerTextE = @"var manager = new CoroutineManager();
        var root = new Sequence(manager);
        var stages = new Queue<string>();

        manager.Root = root;
        Assert.AreEqual(Result.Unknown, manager.Result);
        Assert.AreEqual(0UL, manager.TickCount);
        manager.Update(new State { A = true, B = true, C = true});";

            Assert.AreEqual(innerTextE, document.SelectSingleNode("/__Main/FilterD/NodeA/NodeE").InnerText);
        }

        [TestMethod]
        [DeploymentItem("ParserTests\\testScriptA.ygg")]
        public void ScriptStructureTest()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(ParameterizedTestNode).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var context = parser.BuildFromFiles<TestState>("ParserTests\\testScriptA.ygg");

            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(4, context.TopmostNodeCount);

            // MyCustomTypeA
            var nodeA = context.Instantiate("A");
            Assert.AreEqual("A", nodeA.Guid);
            Assert.AreEqual(typeof(Filter), nodeA.GetType());
            Assert.AreEqual(1, nodeA.Children.Count);

            var nodeB = nodeA.Children[0];
            Assert.AreEqual("B", nodeB.Guid);
            Assert.AreEqual(typeof(Sequence), nodeB.GetType());
            Assert.AreEqual(3, nodeB.Children.Count);

            var nodeC = nodeB.Children[0];
            Assert.AreEqual("C", nodeC.Guid);
            Assert.AreEqual(typeof(Condition), nodeC.GetType());
            Assert.AreEqual(0, nodeC.Children.Count);

            var nodeD = nodeB.Children[1];
            Assert.AreEqual("D", nodeD.Guid);
            Assert.AreEqual(typeof(Condition), nodeD.GetType());
            Assert.AreEqual(0, nodeD.Children.Count);

            var nodeE = nodeB.Children[2];
            Assert.AreEqual("E", nodeE.Guid);
            Assert.AreEqual(typeof(Inverter), nodeE.GetType());
            Assert.AreEqual(1, nodeE.Children.Count);

            var nodeF = nodeE.Children[0];
            Assert.AreEqual("F", nodeF.Guid);
            Assert.AreEqual(typeof(Condition), nodeF.GetType());
            Assert.AreEqual(0, nodeF.Children.Count);

            // MyCustomTypeB
            var nodeG = context.Instantiate("G");
            Assert.AreEqual("G", nodeG.Guid);
            Assert.AreEqual(typeof(Sequence), nodeG.GetType());
            Assert.AreEqual(2, nodeG.Children.Count);

            var nodeH = nodeG.Children[0];
            Assert.AreEqual("H", nodeH.Guid);
            Assert.AreEqual(typeof(Filter), nodeH.GetType());
            CheckMyCustomTypeA(nodeH);

            var nodeI = nodeG.Children[1];
            Assert.AreEqual("I", nodeI.Guid);
            Assert.AreEqual(typeof(Filter), nodeI.GetType());
            CheckMyCustomTypeA(nodeI);

            // Third node.
            var nodeJ = context.Instantiate("J");
            Assert.AreEqual("J", nodeJ.Guid);
            Assert.AreEqual(typeof(Inverter), nodeJ.GetType());
            Assert.AreEqual(1, nodeJ.Children.Count);

            var nodeK = nodeJ.Children[0];
            Assert.AreEqual("K", nodeK.Guid);
            Assert.AreEqual(typeof(Sequence), nodeK.GetType());
            CheckMyCustomTypeB(nodeK);

            // Parameterized node.
            var nodeL = context.Instantiate("L");
            Assert.AreEqual("L", nodeL.Guid);
            Assert.AreEqual(typeof(ParameterizedTestNode), nodeL.GetType());
            Assert.AreEqual(7, nodeL.Children.Count);

            var parameterizedNode = (ParameterizedTestNode)nodeL;
            Assert.AreEqual(1, parameterizedNode.PropertyA);
            Assert.AreEqual(2, parameterizedNode.FieldA);
            Assert.AreEqual("hello", parameterizedNode.PropertyB);
            Assert.AreEqual("goodbye", parameterizedNode.FieldB);
            Assert.AreEqual(3, parameterizedNode.PropertyC);
            Assert.AreEqual(4, parameterizedNode.FieldC);
            Assert.AreEqual(3, parameterizedNode.ArrayPropertyA.Count);
            Assert.AreEqual("one", parameterizedNode.ArrayPropertyA[0].PropertyA);
            Assert.AreEqual("two", parameterizedNode.ArrayPropertyA[1].PropertyA);
            Assert.AreEqual("three", parameterizedNode.ArrayPropertyA[2].PropertyA);
        }

        private static void CheckMyCustomTypeA(Node node)
        {
            Assert.AreEqual(typeof(Filter), node.GetType());
            Assert.AreEqual(1, node.Children.Count);

            var nodeB = node.Children[0];
            Assert.AreEqual(typeof(Sequence), nodeB.GetType());
            Assert.AreEqual(3, nodeB.Children.Count);

            var nodeC = nodeB.Children[0];
            Assert.AreEqual(typeof(Condition), nodeC.GetType());
            Assert.AreEqual(0, nodeC.Children.Count);

            var nodeD = nodeB.Children[1];
            Assert.AreEqual(typeof(Condition), nodeD.GetType());
            Assert.AreEqual(0, nodeD.Children.Count);

            var nodeE = nodeB.Children[2];
            Assert.AreEqual(typeof(Inverter), nodeE.GetType());
            Assert.AreEqual(1, nodeE.Children.Count);

            var nodeF = nodeE.Children[0];
            Assert.AreEqual(typeof(Condition), nodeF.GetType());
            Assert.AreEqual(0, nodeF.Children.Count);
        }

        private static void CheckMyCustomTypeB(Node node)
        {
            Assert.AreEqual(typeof(Sequence), node.GetType());
            Assert.AreEqual(2, node.Children.Count);

            var nodeH = node.Children[0];
            Assert.AreEqual(typeof(Filter), nodeH.GetType());
            CheckMyCustomTypeA(nodeH);

            var nodeI = node.Children[1];
            Assert.AreEqual(typeof(Filter), nodeI.GetType());
            CheckMyCustomTypeA(nodeI);
        }

        public class TestState
        {
            public int A, B, C, D, E;
            public string FirstName, SecondName, ThirdName;
        }

        public class ParameterizedTestNode : Node
        {
            [XmlAttribute]
            public int PropertyA { get; set; }

            [XmlAttribute]
            public int FieldA;

            [XmlAttribute]
            public string PropertyB { get; set; }

            [XmlAttribute]
            public string FieldB;

            [XmlElement]
            public int PropertyC { get; set; }

            [XmlElement]
            public int FieldC { get; set; }

            [XmlArray]
            [XmlArrayItem(nameof(TestArrayItem))]
            public List<TestArrayItem> ArrayPropertyA { get; set; }

            protected override Coroutine<Result> Tick()
            {
                return Success;
            }
        }

        [XmlType]
        public class TestArrayItem
        {
            [XmlAttribute]
            public string PropertyA { get; set; }
        }
    }
}
