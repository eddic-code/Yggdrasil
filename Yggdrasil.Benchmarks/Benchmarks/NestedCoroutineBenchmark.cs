using BenchmarkDotNet.Attributes;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(baseline: true)]
    [MemoryDiagnoser]
    public class NestedCoroutineBenchmark
    {
        private CoroutineManager _manager;

        [GlobalSetup]
        public void Setup()
        {
            _manager = new CoroutineManager();
            _manager.Root = new NestedCoroutinesTestNode(_manager);

            while (_manager.TickCount == 0) { _manager.Tick(); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Tick(); }
        }

        private class NestedCoroutinesTestNode : Node
        {
            public NestedCoroutinesTestNode(CoroutineManager tree) : base(tree)
            {

            }

            public override async Coroutine Tick()
            {
                await Yield;

                await Yield;
            }
        }
    }
}
