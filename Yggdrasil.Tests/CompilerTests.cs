using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;
using Yggdrasil.Scripting;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class CompilerTests
    {
        [TestMethod]
        public void DynamicFunctionCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";
            const string textD = @"
            dynamic state = new ExpandoObject();
            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            return state;";

            var config = new YggParserConfig();
            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);

            var property = typeof(TestDynamicConditionDouble).GetProperty("Conditional");
            var propertySingle = typeof(TestDynamicConditionSingle).GetProperty("Conditional");

            var conditionA = new TestDynamicConditionDouble();
            var conditionB = new TestDynamicConditionDouble();
            var conditionC = new TestDynamicConditionDouble();
            var conditionD = new TestDynamicConditionSingle();

            var dA = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, 
                FunctionText = textA, ReplaceObjectWithDynamic = true};
            var dB = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, 
                FunctionText = textB, ReplaceObjectWithDynamic = true};
            var dC = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, 
                FunctionText = textC, ReplaceObjectWithDynamic = true};
            var dD = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = propertySingle, 
                FunctionText = textD, ReplaceObjectWithDynamic = true};

            var compiler = new YggCompiler();
            var definitions = new List<ScriptedFunctionDefinition> {dA, dB, dC, dD};
            var compilation = compiler.Compile<object>(config.ScriptNamespaces, config.ReferenceAssemblyPaths, definitions);

            Assert.AreEqual(0, compilation.Errors.Count);
            Assert.AreEqual(4, compilation.GuidFunctionMap.Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dA.Guid].Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dB.Guid].Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dC.Guid].Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dD.Guid].Count);

            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            compilation.GuidFunctionMap[dA.Guid][0].SetFunctionPropertyValue(conditionA);

            Assert.IsNotNull(conditionA.Conditional);
            Assert.IsTrue(conditionA.Conditional(state));

            compilation.GuidFunctionMap[dB.Guid][0].SetFunctionPropertyValue(conditionB);

            Assert.IsNotNull(conditionB.Conditional);
            Assert.IsFalse(conditionB.Conditional(state));

            compilation.GuidFunctionMap[dC.Guid][0].SetFunctionPropertyValue(conditionC);

            Assert.IsNotNull(conditionC.Conditional);
            Assert.IsTrue(conditionC.Conditional(state));

            compilation.GuidFunctionMap[dD.Guid][0].SetFunctionPropertyValue(conditionD);

            Assert.IsNotNull(conditionD.Conditional);

            var result = conditionD.Conditional();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.A);
            Assert.AreEqual(2, result.B);
            Assert.AreEqual(3, result.C);
            Assert.AreEqual(4, result.D);
            Assert.AreEqual(5, result.E);
        }

        [TestMethod]
        public void FunctionCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var config = new YggParserConfig();
            config.NodeTypeAssemblies.Add(typeof(TestState).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);

            var conditionA = new Condition();
            var conditionB = new Condition();
            var conditionC = new Condition();

            var property = typeof(Condition).GetProperty("Conditional");

            var dA = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, FunctionText = textA};
            var dB = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, FunctionText = textB};
            var dC = new ScriptedFunctionDefinition{Guid = Guid.NewGuid().ToString().Replace("-", ""), FunctionProperty = property, FunctionText = textC};

            var compiler = new YggCompiler();
            var definitions = new List<ScriptedFunctionDefinition> {dA, dB, dC};
            var compilation = compiler.Compile<TestState>(config.ScriptNamespaces, config.ReferenceAssemblyPaths, definitions);

            Assert.AreEqual(0, compilation.Errors.Count);
            Assert.AreEqual(3, compilation.GuidFunctionMap.Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dA.Guid].Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dB.Guid].Count);
            Assert.AreEqual(1, compilation.GuidFunctionMap[dC.Guid].Count);

            var state = new TestState();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            compilation.GuidFunctionMap[dA.Guid][0].SetFunctionPropertyValue(conditionA);

            Assert.IsNotNull(conditionA.Conditional);
            Assert.IsTrue(conditionA.Conditional(state));

            compilation.GuidFunctionMap[dB.Guid][0].SetFunctionPropertyValue(conditionB);

            Assert.IsNotNull(conditionB.Conditional);
            Assert.IsFalse(conditionB.Conditional(state));

            compilation.GuidFunctionMap[dC.Guid][0].SetFunctionPropertyValue(conditionC);

            Assert.IsNotNull(conditionC.Conditional);
            Assert.IsTrue(conditionC.Conditional(state));
        }

        public class TestState
        {
            public int A, B, C, D, E;
            public string FirstName, SecondName, ThirdName;
        }

        public class TestDynamicConditionDouble : Node
        {
            public Func<dynamic, bool> Conditional { get; set; }

            protected override Coroutine<Result> Tick()
            {
                throw new NotImplementedException();
            }
        }

        public class TestDynamicConditionSingle: Node
        {
            public Func<dynamic> Conditional { get; set; }

            protected override Coroutine<Result> Tick()
            {
                throw new NotImplementedException();
            }
        }
    }
}
