using BenchmarkDotNet.Running;

namespace Yggdrasil.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //ProfilerTests();
        }

        public static void ProfilerTests()
        {
            SequenceNodeBenchmark();
            GenericCoroutineBenchmark();
            NestedCoroutineBenchmark();
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
    }
}
