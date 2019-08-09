using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Nodes;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
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

                await MethodA();

                await Yield;

                await MethodB();
                await MethodC();

                await Yield;

                await MethodD();
                await MethodA();

                await Yield;
            }

            private async Coroutine MethodA()
            {
                await Yield;

                await MethodB();

                await Yield;
            }

            private async Coroutine MethodB()
            {
                await Yield;
            }

            private async Coroutine MethodC()
            {
                await Yield;
            }

            // No awaits inside on purpose.
#pragma warning disable 1998
            private async Coroutine MethodD()
#pragma warning restore 1998
            {

            }
        }
    }
}
