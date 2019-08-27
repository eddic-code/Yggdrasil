using BenchmarkDotNet.Running;

namespace Yggdrasil.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //ProfilerTest();
        }

        public static void ProfilerTest()
        {
            var s = new GenericCoroutineBenchmark();

            s.Setup();
            s.Execute();
        }
    }
}
