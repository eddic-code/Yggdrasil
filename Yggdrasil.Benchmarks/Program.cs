using BenchmarkDotNet.Running;

namespace Yggdrasil.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            //var summary = BenchmarkRunner.Run<NestedCoroutineBenchmark>();

            var s = new NestedCoroutineBenchmark();

            s.Setup();
            s.Execute();
            s.Cleanup();
        }
    }
}
