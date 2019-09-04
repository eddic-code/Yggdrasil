using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;
using Yggdrasil.Scripting;

namespace Yggdrasil.Tests
{
    [TestClass]
    public class YggCompilerTests
    {
        [TestMethod]
        public void DynamicFunctionCompilationTest()
        {
            const string textA = @"state.A >= state.B || state.C <= state.D";
            const string textB = @"state.A >= state.B || state.C >= state.D";
            const string textC = @"state.FirstName != state.SecondName && state.FirstName == state.ThirdName";

            var parser = new YggCompiler();
            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var conditionA = new TestDynamicConditionDouble();
            var conditionB = new TestDynamicConditionDouble();
            var conditionC = new TestDynamicConditionDouble();

            var errors = parser.CompileDynamicFunction(conditionA, "Conditional", textA);
            Assert.AreEqual(0, errors.Length);
            Assert.IsNotNull(conditionA.Conditional);
            Assert.IsTrue(conditionA.Conditional(state));

            errors = parser.CompileDynamicFunction(conditionB, "Conditional", textB);
            Assert.AreEqual(0, errors.Length);
            Assert.IsNotNull(conditionB.Conditional);
            Assert.IsFalse(conditionB.Conditional(state));

            errors = parser.CompileDynamicFunction(conditionC, "Conditional", textC);
            Assert.AreEqual(0, errors.Length);
            Assert.IsNotNull(conditionC.Conditional);
            Assert.IsTrue(conditionC.Conditional(state));

            const string scriptText = @"
            dynamic state = new ExpandoObject();
            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            return state;";

            var conditionD = new TestDynamicConditionSingle();

            errors = parser.CompileDynamicFunction(conditionD, "Conditional", scriptText);
            Assert.AreEqual(0, errors.Length);
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

            var parser = new YggCompiler();
            var state = new TestState();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            state.E = 5;
            state.FirstName = "dimitri";
            state.SecondName = "edelgard";
            state.ThirdName = "dimitri";

            var conditionA = new Condition();
            var conditionB = new Condition();
            var conditionC = new Condition();

            var errors = parser.CompileFunction<TestState>(conditionA, "Conditional", textA);
            Assert.AreEqual(0, errors.Length);
            Assert.IsNotNull(conditionA.Conditional);
            Assert.IsTrue(conditionA.Conditional(state));

            errors = parser.CompileFunction<TestState>(conditionB, "Conditional", textB);
            Assert.AreEqual(0, errors.Length);
            Assert.IsNotNull(conditionB.Conditional);
            Assert.IsFalse(conditionB.Conditional(state));

            errors = parser.CompileFunction<TestState>(conditionC, "Conditional", textC);
            Assert.AreEqual(0, errors.Length);
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
