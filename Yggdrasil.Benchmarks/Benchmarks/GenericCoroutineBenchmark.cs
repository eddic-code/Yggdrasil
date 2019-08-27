using BenchmarkDotNet.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Nodes;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class GenericCoroutineBenchmark
    {
        private CoroutineManager _manager;

        [GlobalSetup]
        public void Setup()
        {
            _manager = new CoroutineManager();
            _manager.Root = new GenericCoroutineTestNode(_manager);

            while (_manager.TickCount == 0) { _manager.Tick(); }
        }

        [Benchmark]
        public void Execute()
        {
            while (_manager.TickCount == 1) { _manager.Tick(); }
        }

        private class GenericCoroutineTestNode : Node
        {
            public GenericCoroutineTestNode(CoroutineManager manager) : base(manager)
            {

            }

            protected override async Coroutine<Result> Tick()
            {
                await Yield;

                var result = await MethodA();

                result = await MethodB(result);

                await MethodC(result == 50);

                await Yield;

                return Result.Success;
            }

            private async Coroutine<int> MethodA()
            {
                await Yield;

                var result = await MethodB(5);

                await Yield;

                return 10 * result;
            }

            private async Coroutine<int> MethodB(int input)
            {
                await Yield;

                return input;
            }

            private async Coroutine<bool> MethodC(bool input)
            {
                await Yield;

                return !input;
            }
        }
    }
}