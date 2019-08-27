using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
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
            _manager.Root = new NestedCoroutineTestNode(_manager);

            while (_manager.TickCount == 0) { _manager.Update(); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Update(); }
        }

        private class NestedCoroutineTestNode : Node
        {
            public NestedCoroutineTestNode(CoroutineManager manager) : base(manager)
            {

            }

            protected override async Coroutine<Result> Tick()
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

                return Result.Success;
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

            // No awaits inside for testing purposes.
#pragma warning disable 1998
            private async Coroutine MethodD()
#pragma warning restore 1998
            {

            }
        }
    }
}
