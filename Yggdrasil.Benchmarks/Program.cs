using BenchmarkDotNet.Running;

namespace Yggdrasil.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            //var benchmark = new RoslynScriptingBenchmark();

            //benchmark.Setup();
            //benchmark.WrappedDynamicStateConditional();
            //benchmark.GenericStateCompiledConditional();
            //benchmark.DynamicStateCompiledConditional();

            //ProfilerTests();
        }

        public static void ProfilerTests()
        {
            SequenceNodeBenchmark();
            GenericCoroutineBenchmark();
            NestedCoroutineBenchmark();
            ParallelNodeBenchmark();
            NestedParallelNodeBenchmark();
            DynamicSequenceNodeBenchmark();
        }

        public static void SequenceNodeBenchmark()
        {
            var s = new SequenceNodeBenchmark();

            s.Setup();
            s.Execute();
        }

        public static void GenericCoroutineBenchmark()
        {
            var s = new GenericCoroutineBenchmark();

            s.Setup();
            s.Execute();
        }

        public static void NestedCoroutineBenchmark()
        {
            var s = new NestedCoroutineBenchmark();

            s.Setup();
            s.Execute();
        }

        public static void ParallelNodeBenchmark()
        {
            var s = new ParallelNodeBenchmark();

            s.Setup();
            s.Execute();
        }

        public static void NestedParallelNodeBenchmark()
        {
            var s = new NestedParallelNodeBenchmark();

            s.Setup();
            s.Execute();
        }

        public static void DynamicSequenceNodeBenchmark()
        {
            var s = new DynamicSequenceNodeBenchmark();

            s.Setup();
            s.Execute();
        }
    }
}
