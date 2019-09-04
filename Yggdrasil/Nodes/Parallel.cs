#region License

// /*
// MIT License
// 
// Copyright (c) 2019 eddic-code
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// */

#endregion

using System.Collections.Generic;
using System.Xml.Serialization;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Parallel : Node
    {
        [XmlIgnore]
        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>(10);

        [XmlIgnore]
        private List<Node> _children;

        public Parallel(CoroutineManager manager) : base(manager) { }

        [XmlIgnore]
        public override List<Node> Children
        {
            get => _children;
            set
            {
                _children = value;
                _threads.Clear();

                if (value != null && value.Count > 0)
                {
                    foreach (var n in value)
                    {
                        var thread = new CoroutineThread(n, false, 1);
                        _threads.Add(thread);
                    }
                }
            }
        }

        public override void Terminate()
        {
            foreach (var thread in _threads) { thread.Reset(); }
        }

        protected override async Coroutine<Result> Tick()
        {
            foreach (var thread in _threads) { Manager.ProcessThreadAsDependency(thread); }

            while (Continue()) { await Yield; }

            var result = Result.Failure;

            foreach (var thread in _threads)
            {
                if (thread.Result == Result.Success) { result = Result.Success; }

                thread.Reset();
            }

            return result;
        }

        private bool Continue()
        {
            var processing = false;
            foreach (var thread in _threads) { processing = processing || !thread.IsComplete; }

            return processing;
        }
    }
}