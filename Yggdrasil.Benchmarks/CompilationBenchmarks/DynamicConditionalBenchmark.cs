using System;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Serialization;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class DynamicConditionalBenchmark
    {
        private Func<dynamic, bool> _conditional;
        private Func<dynamic, bool> _conditional2;
        private object _state;

        [GlobalSetup]
        public void Setup()
        {
            dynamic state = new ExpandoObject();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;

            _state = state;

            const string scriptText = @"state.A >= state.B || state.C <= state.D";
            var compiler = new YggCompiler();

            _conditional = compiler.CompileDynamicConditional(scriptText);
            _conditional2 = s => s.A >= s.B || s.C <= s.D;
        }

        [Benchmark]
        public void Execute()
        {
            _conditional(_state);
        }

        [Benchmark]
        public void TypedFunction()
        {
            _conditional2(_state);
        }

        [Benchmark]
        public void StaticMethod()
        {
            Conditional(_state);
        }

        private static bool Conditional(dynamic state)
        {
            return state.A >= state.B || state.C <= state.D;
        }
    }
}
