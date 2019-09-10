using System;
using System.IO;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using Yggdrasil.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;
using Yggdrasil.Behaviour;
using Yggdrasil.Scripting;

namespace Yggdrasil.Benchmarks
{
    [CoreJob(true)]
    [MemoryDiagnoser]
    public class ScriptBenchmarks
    {
        private BehaviourTree _tree;
        private TestState _state;

        [GlobalSetup]
        public void Setup()
        {
            var config = new YggParserConfig();

            config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
            config.NodeTypeAssemblies.Add(typeof(TestRunningAction).Assembly.GetName().Name);

            var compiler = new YggCompiler();
            var parser = new YggParser(config, compiler);
            var scriptPath = Path.Combine(Environment.CurrentDirectory, "BenchmarkScripts\\testScriptB.ygg");
            var context = parser.BuildFromFiles<TestState>(scriptPath);
            if (context.Errors.Count > 0) { throw new Exception(context.Errors[0].Message); }

            _tree = new BehaviourTree(context.Instantiate("root"));
            _state = new TestState();

            // Warmup.
            while (_tree.TickCount == 0) { _tree.Update(_state); }

            _state = new TestState();
        }

        [Benchmark]
        public void Execute()
        {
            _tree.Update(_state);
            _tree.Update(_state);
            _tree.Update(_state);
            _tree.Update(_state);
            _tree.Update(_state);
            _tree.Update(_state);
        }

        public class TestState
        {
            public bool EventHappened;
            public int A, B, C, D, E;
            public string FirstName, SecondName, ThirdName;
        }

        public class TestRunningAction : Node
        {
            [XmlAttribute]
            public Result Output { get; set; }

            [XmlIgnore]
            [ScriptedFunction]
            public Action<object> Function { get; set; } = DefaultFunction;

            [XmlAttribute]
            public int Yields { get; set; }

            protected override async Coroutine<Result> Tick()
            {
                for (var i = 0; i < Yields; i++)
                {
                    Function(State);
                    await Yield;
                }

                return Output;
            }

            private static void DefaultFunction(object state)
            {

            }
        }
    }
}
