using System;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Serialization;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class CompiledGenericConditional
    {
        private Delegate _conditional;
        private State _state;

        [GlobalSetup]
        public void Setup()
        {
            var state = new State();

            state.A = 1;
            state.B = 2;
            state.C = 3;
            state.D = 4;

            _state = state;

            const string scriptText = @"state.A >= state.B || state.C <= state.D";
            var compiler = new YggCompiler();

            _conditional = compiler.CompileGenericConditional<State>(scriptText);
            _conditional.DynamicInvoke(_state);
        }

        [Benchmark]
        public void Execute()
        {
            _conditional.DynamicInvoke(_state);
        }
    }
}