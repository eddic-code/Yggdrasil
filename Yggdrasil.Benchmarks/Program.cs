﻿using System;
using BenchmarkDotNet.Running;

namespace Yggdrasil.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var benchmark = new ScriptBenchmarks();
            benchmark.Setup();
            benchmark.Execute();

            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            //Console.ReadLine();
        }
    }
}
