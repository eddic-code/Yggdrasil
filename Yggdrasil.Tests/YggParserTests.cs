using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Scripting;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class YggParserTests
    {
        [TestMethod]
        [DeploymentItem("ParserTests\\escapeCharacterTest.ygg")]
        public void EscapeCharacterTest()
        {
            var parser = new YggParser();
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
        public void ConditionalCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var parser = new YggParser();
            var state = new TestState();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var scriptA = parser.CompileConditional<TestState>(textA);
            Assert.IsTrue(scriptA.Execute(state));

            var scriptB = parser.CompileConditional<TestState>(textB);
            Assert.IsFalse(scriptB.Execute(state));

            var scriptC = parser.CompileConditional<TestState>(textC);
            Assert.IsTrue(scriptC.Execute(state));
        }

        [TestMethod]
        public void DynamicConditionalCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var parser = new YggParser();
            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var scriptA = parser.CompileDynamicConditional(textA);
            Assert.IsTrue(scriptA.Execute(state));

            var scriptB = parser.CompileDynamicConditional(textB);
            Assert.IsFalse(scriptB.Execute(state));

            var scriptC = parser.CompileDynamicConditional(textC);
            Assert.IsTrue(scriptC.Execute(state));
        }

        public class TestState
        {
            public int A, B, C, D, E;
            public string FirstName, SecondName, ThirdName;
        }
    }
}
