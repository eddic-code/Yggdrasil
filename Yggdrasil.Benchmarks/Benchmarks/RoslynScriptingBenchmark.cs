using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class RoslynScriptingBenchmark
    {
        private object _dynamicState;
        private TestGenericState _testGenericState;

        private TestBaseConditional _derivedConditional;
        private TestBaseDynamicConditional _derivedDynamicConditional;
        private Func<TestGenericState, bool> _derivedFunction;
        private Func<dynamic, bool> _derivedDynamicFunction;

        private Func<TestGenericState, bool> _baseline;
        private Func<TestGenericState, bool> _staticMethod;

        private Func<dynamic, bool> _dynamicBaseline;
        private Func<dynamic, bool> _dynamicStaticMethod;
        private Func<dynamic, bool> _wrappedConditional;

        private Func<dynamic, bool> _dynamicCompiledConditionalFunc;
        private Func<dynamic, bool> _genericCompiledConditionalFunc;

        private Delegate _dynamicCompiledConditional;
        private Delegate _genericCompiledConditional;

        [GlobalSetup]
        public void Setup()
        {
            _testGenericState = new TestGenericState {A = 1, B = 2, C = 3, D = 4};

            dynamic state = new ExpandoObject();
            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            _dynamicState = state;

            const string script = "state.A >= state.B || state.C <= state.D";

            var dynamicConditional = DynamicStateCompiledConditional(script);
            var genericConditional = GenericStateCompiledConditional<TestGenericState>(script);

            _baseline = s => s.A >= s.B || s.C <= s.D;
            _staticMethod = Conditional;
            _dynamicStaticMethod = DynamicConditional;
            _dynamicBaseline = s => s.A >= s.B || s.C <= s.D;
            _wrappedConditional = WrappedDynamicStateConditional(script);
            _dynamicCompiledConditional = dynamicConditional;
            _genericCompiledConditional = genericConditional;
            _dynamicCompiledConditionalFunc = s => dynamicConditional.DynamicInvoke(s);
            _genericCompiledConditionalFunc = s => genericConditional.DynamicInvoke(s);

            var derivedConditional = GenericStateCompiledInheritedConditional<TestGenericState>(script);
            _derivedConditional = derivedConditional;
            _derivedFunction = s => derivedConditional.Execute(s);

            var derivedDynamicConditional = DynamicStateCompiledInheritedConditional(script);
            _derivedDynamicConditional = derivedDynamicConditional;
            _derivedDynamicFunction = s => derivedDynamicConditional.Execute(s);

            // Warmup.
            _dynamicBaseline(_dynamicState);
            _dynamicStaticMethod(_dynamicState);
            _wrappedConditional.DynamicInvoke(_dynamicState);
            _dynamicCompiledConditional.DynamicInvoke(_dynamicState);
            _genericCompiledConditional.DynamicInvoke(_testGenericState);
            _dynamicCompiledConditionalFunc(_dynamicState);
            _genericCompiledConditionalFunc(_testGenericState);
            _derivedConditional.Execute(_testGenericState);
            _derivedDynamicConditional.Execute(_dynamicState);
            _derivedFunction(_testGenericState);
            _derivedDynamicFunction(_dynamicState);
        }

        private static bool DynamicConditional(dynamic state)
        {
            return state.A >= state.B || state.C <= state.D;
        }

        private static bool Conditional(TestGenericState state)
        {
            return state.A >= state.B || state.C <= state.D;
        }

        [Benchmark]
        public void BBaseline()
        {
            _baseline(_testGenericState);
        }

        [Benchmark]
        public void BStaticMethod()
        {
            _staticMethod(_testGenericState);
        }

        [Benchmark]
        public void BDynamicBaseline()
        {
            _dynamicBaseline(_dynamicState);
        }

        [Benchmark]
        public void BDynamicStaticMethod()
        {
            _dynamicStaticMethod(_dynamicState);
        }

        [Benchmark]
        public void BWrappedDynamicStateConditional()
        {
            _wrappedConditional.DynamicInvoke(_dynamicState);
        }

        [Benchmark]
        public void BDynamicStateCompiledConditionalDelegate()
        {
            _dynamicCompiledConditional.DynamicInvoke(_dynamicState);
        }

        [Benchmark]
        public void BGenericStateCompiledConditionalDelegate()
        {
            _genericCompiledConditional.DynamicInvoke(_testGenericState);
        }

        [Benchmark]
        public void BDynamicStateCompiledConditionalFunction()
        {
            _dynamicCompiledConditionalFunc(_dynamicState);
        }

        [Benchmark]
        public void BGenericStateCompiledConditionalFunction()
        {
            _genericCompiledConditionalFunc(_testGenericState);
        }

        [Benchmark]
        public void BDerivedConditional()
        {
            _derivedConditional.Execute(_testGenericState);
        }

        [Benchmark]
        public void BDerivedDynamicConditional()
        {
            _derivedDynamicConditional.Execute(_dynamicState);
        }

        [Benchmark]
        public void BDerivedFunction()
        {
            _derivedFunction(_testGenericState);
        }

        [Benchmark]
        public void BDerivedDynamicFunction()
        {
            _derivedDynamicFunction(_dynamicState);
        }

        private static Func<dynamic, bool> WrappedDynamicStateConditional(string text)
        {
            var references = new List<MetadataReference>{
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location)};

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(text, options, typeof(StateWrapper));

            script.Compile();

            return s => script.RunAsync(new StateWrapper { state = s }).Result.ReturnValue;
        }

        private static Delegate DynamicStateCompiledConditional(string functionText)
        {
            var scriptText = $"using System.Dynamic; public static class YggEntry {{ public static bool Conditional(dynamic state){{ return {functionText}; }} }}";

            var references = new List<MetadataReference>{
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location)};

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "YggEntry");

            var method = entryType.GetMethod("Conditional");
            if (method == null) { throw new Exception(); }

            var function = Delegate.CreateDelegate(typeof(Func<dynamic, bool>), method);

            return function;
        }

        private static Delegate GenericStateCompiledConditional<T>(string functionText)
        {
            var scriptText = $"using Yggdrasil.Benchmarks; public static class YggEntry {{ public static bool Conditional({typeof(T).Name} state){{ return {functionText}; }} }}";

            var references = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(T).GetTypeInfo().Assembly.Location) };
            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "YggEntry");

            var method = entryType.GetMethod("Conditional");
            if (method == null) { throw new Exception(); }

            var function = Delegate.CreateDelegate(typeof(Func<T, bool>), method);

            return function;
        }

        private static TestBaseConditional GenericStateCompiledInheritedConditional<T>(string functionText)
        {
            var scriptText = $"using Yggdrasil.Benchmarks; public class YggEntry : TestBaseConditional {{ public override bool Execute(TestGenericState state){{ return {functionText}; }} }}";

            var references = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(T).GetTypeInfo().Assembly.Location) };
            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "YggEntry");
            var instance = (TestBaseConditional)Activator.CreateInstance(entryType);

            return instance;
        }

        private TestBaseDynamicConditional DynamicStateCompiledInheritedConditional(string functionText)
        {
            var scriptText = $"using Yggdrasil.Benchmarks; using System.Dynamic; public class YggEntry : TestBaseDynamicConditional {{ public override bool Execute(dynamic state){{ return {functionText}; }} }}";

            var references = new List<MetadataReference>{
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TestGenericState).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location)};

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "YggEntry");
            var instance = (TestBaseDynamicConditional)Activator.CreateInstance(entryType);

            return instance;
        }

        public class StateWrapper
        {
            // ReSharper disable once InconsistentNaming
            public dynamic state;
        }
    }

    public abstract class TestBaseDynamicConditional
    {
        public abstract bool Execute(dynamic state);
    }

    public abstract class TestBaseConditional
    {
        public abstract bool Execute(TestGenericState state);
    }

    public class TestGenericState
    {
        public int A, B, C, D;
    }
}
