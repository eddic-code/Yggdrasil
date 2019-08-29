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
        private GenericState _genericState;

        private Func<dynamic, bool> _conditionalBaseline;
        private Func<dynamic, bool> _staticMethod;
        private Func<dynamic, bool> _wrappedConditional;

        private Func<dynamic, bool> _dynamicCompiledConditionalFunc;
        private Func<dynamic, bool> _genericCompiledConditionalFunc;

        private Delegate _dynamicCompiledConditional;
        private Delegate _genericCompiledConditional;

        [GlobalSetup]
        public void Setup()
        {
            _genericState = new GenericState {A = 1, B = 2, C = 3, D = 4};

            dynamic state = new ExpandoObject();
            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;
            _dynamicState = state;

            const string script = "state.A >= state.B || state.C <= state.D";

            var dynamicConditional = DynamicStateCompiledConditional(script);
            var genericConditional = GenericStateCompiledConditional<GenericState>(script);

            _staticMethod = Conditional;

            _conditionalBaseline = s => s.A >= s.B || s.C <= s.D;
            _wrappedConditional = WrappedDynamicStateConditional(script);
            _dynamicCompiledConditional = dynamicConditional;
            _genericCompiledConditional = genericConditional;
            _dynamicCompiledConditionalFunc = s => dynamicConditional.DynamicInvoke(s);
            _genericCompiledConditionalFunc = s => genericConditional.DynamicInvoke(s);

            // Warmup.
            _conditionalBaseline(_dynamicState);
            _staticMethod(_dynamicState);
            _wrappedConditional.DynamicInvoke(_dynamicState);
            _dynamicCompiledConditional.DynamicInvoke(_dynamicState);
            _genericCompiledConditional.DynamicInvoke(_genericState);
            _dynamicCompiledConditionalFunc(_dynamicState);
            _genericCompiledConditionalFunc(_genericState);
        }

        private static bool Conditional(dynamic state)
        {
            return state.A >= state.B || state.C <= state.D;
        }

        [Benchmark]
        public void Baseline()
        {
            _conditionalBaseline(_dynamicState);
        }

        [Benchmark]
        public void StaticMethod()
        {
            _staticMethod(_dynamicState);
        }

        [Benchmark]
        public void WrappedDynamicStateConditional()
        {
            _wrappedConditional.DynamicInvoke(_dynamicState);
        }

        [Benchmark]
        public void DynamicStateCompiledConditionalDelegate()
        {
            _dynamicCompiledConditional.DynamicInvoke(_dynamicState);
        }

        [Benchmark]
        public void GenericStateCompiledConditionalDelegate()
        {
            _genericCompiledConditional.DynamicInvoke(_genericState);
        }

        [Benchmark]
        public void DynamicStateCompiledConditionalFunction()
        {
            _dynamicCompiledConditionalFunc(_dynamicState);
        }

        [Benchmark]
        public void GenericStateCompiledConditionalFunction()
        {
            _genericCompiledConditionalFunc(_genericState);
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

        public class StateWrapper
        {
            // ReSharper disable once InconsistentNaming
            public dynamic state;
        }
    }

    public class GenericState
    {
        public int A, B, C, D;
    }
}
