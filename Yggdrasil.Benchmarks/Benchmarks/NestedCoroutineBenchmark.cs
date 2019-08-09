using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(baseline: true)]
    [MemoryDiagnoser]
    public class NestedCoroutineBenchmark
    {
        private CoroutineManager _manager;
        private List<int> _stuff;

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

        [GlobalCleanup]
        public void Cleanup()
        {
            _manager.Dispose();
        }

        private class NestedCoroutinesTestNode : Node
        {
            public NestedCoroutinesTestNode(CoroutineManager tree) : base(tree)
            {

            }

            public override async Coroutine Tick()
            {
                await Yield;
            }

            private async Coroutine Method2()
            {
                await Yield;
            }

            private async Coroutine Method3()
            {
                await Yield;

                await Yield;
            }

            //private async Coroutine<int> Method2()
            //{
            //    await Yield;

            //    return 10;
            //}

            //private async Coroutine Method3(int value)
            //{
            //    value += 1;

            //    await Yield;

            //    value += 1;
            //}
        }
    }
}
