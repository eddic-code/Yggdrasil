using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Serialization;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class XmlParserTests
    {
        [TestMethod]
        [DeploymentItem("XmlTests\\escapeCharacterTest.xml")]
        public void EscapeCharacterTest()
        {
            var document = CustomXmlParser.LoadFromFile("XmlTests\\escapeCharacterTest.xml");
            
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
    }
}
