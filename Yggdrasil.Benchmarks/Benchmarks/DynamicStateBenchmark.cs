using System;
using System.Dynamic;
using BenchmarkDotNet.Attributes;

namespace Yggdrasil.Benchmarks
{
    public class DynamicStateBenchmark
    {
        private object _state;
        private Func<dynamic, bool> _conditional;

        [GlobalSetup]
        public void Setup()
        {
            dynamic state = new ExpandoObject();
            state.A = true;
            _state = state;

            _conditional = s => s.A;
            _conditional(_state);
        }

        [Benchmark]
        public void Execute()
        {
            _conditional(_state);
        }
    }
}
