using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Serialization;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class YggParserTests
    {
        [TestMethod]
        [DeploymentItem("ParserTests\\escapeCharacterTest.ygg")]
        public void EscapeCharacterTest()
        {
            var document = YggParser.LoadFromFile("ParserTests\\escapeCharacterTest.ygg");
            
            const string innerTextA = @"state.A >= state.B || state.C <= state.D";
            Assert.AreEqual(innerTextA, document.SelectSingleNode("/Nodes/FilterA/Conditional").InnerText);

            const string attributeTextB = @"state.A >= state.B || state.C <= state.D";
            Assert.AreEqual(attributeTextB, document.SelectSingleNode("/Nodes/FilterB").Attributes[0].Value);

            const string attributeTextC = "state.A == \"hello\"";
            Assert.AreEqual(attributeTextC, document.SelectSingleNode("/Nodes/FilterC").Attributes[0].Value);

            const string attributeTextD = "state.A == \"hello\" && state.C <= state.N";
            Assert.AreEqual(attributeTextD, document.SelectSingleNode("/Nodes/FilterD").Attributes[0].Value);

            const string attributeTextA0 = "state.A == \"hello\" && state.C <= state.N || state.D > 10";
            Assert.AreEqual(attributeTextA0, document.SelectSingleNode("/Nodes/FilterD/A").Attributes[0].Value);
            Assert.AreEqual(attributeTextA0, document.SelectSingleNode("/Nodes/FilterD/A").Attributes[1].Value);

            const string attributeTextB0 = "state.A >= state.B || state.C <= state.D";
            const string attributeTextB1 = "state.A > state.B || state.C < state.D";

            Assert.AreEqual(attributeTextB0, document.SelectSingleNode("/Nodes/FilterD/A/B").Attributes[0].Value);
            Assert.AreEqual(attributeTextB1, document.SelectSingleNode("/Nodes/FilterD/A/B").InnerText);

            Assert.AreEqual(attributeTextB1, document.SelectSingleNode("/Nodes/FilterD/A/C").Attributes[0].Value);
            Assert.AreEqual(attributeTextB0, document.SelectSingleNode("/Nodes/FilterD/A/C").InnerText);
        }

        [TestMethod]
        public void ConditionalCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var compiler = new YggCompiler();
            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var scriptA = compiler.DynamicStateCompiledConditional(textA);
            Assert.IsTrue(scriptA.DynamicInvoke(state));

            var scriptB = compiler.DynamicStateCompiledConditional(textB);
            Assert.IsFalse(scriptB.DynamicInvoke(state));

            var scriptC = compiler.DynamicStateCompiledConditional(textC);
            Assert.IsTrue(scriptC.DynamicInvoke(state));
        }

        [TestMethod]
        public void ConditionalCompilationFullTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var compiler = new YggCompiler();
            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var scriptA = compiler.DynamicStateCompiledConditional(textA);
            Assert.IsTrue(scriptA.DynamicInvoke(state));

            var scriptB = compiler.DynamicStateCompiledConditional(textB);
            Assert.IsFalse(scriptB.DynamicInvoke(state));

            var scriptC = compiler.DynamicStateCompiledConditional(textC);
            Assert.IsTrue(scriptC.DynamicInvoke(state));
        }
    }
}
