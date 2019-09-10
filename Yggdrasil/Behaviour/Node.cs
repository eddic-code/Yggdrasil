#region License

// // /*
// // MIT License
// //
// // Copyright (c) 2019 eddic-code
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy
// // of this software and associated documentation files (the "Software"), to deal
// // in the Software without restriction, including without limitation the rights
// // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// // copies of the Software, and to permit persons to whom the Software is
// // furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all
// // copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// // SOFTWARE.
// //
// // */

#endregion

using System.Collections.Generic;
using System.Xml.Serialization;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Behaviour
{
    public abstract class Node
    {
        // References to the thread static behaviour tree instance to keep node instances stateless.
        // Behaviour tree static instance is always the one being currently updated.

        [XmlIgnore]
        protected Coroutine Yield => BehaviourTree.CurrentInstance.Yield;

        [XmlIgnore]
        protected Coroutine<Result> Success => BehaviourTree.CurrentInstance.Success;

        [XmlIgnore]
        protected static Coroutine<Result> Failure => BehaviourTree.CurrentInstance.Failure;

        [XmlIgnore]
        protected object State => BehaviourTree.CurrentInstance.State;

        [XmlIgnore]
        public virtual List<Node> Children { get; set; } = new List<Node>();

        [XmlIgnore]
        public string Guid { get; set; }

        public IEnumerable<Node> DepthFirstIterate()
        {
            var open = new Stack<Node>();
            open.Push(this);

            while (open.Count > 0)
            {
                var next = open.Pop();
                yield return next;

                if (next.Children == null) { continue; }
                foreach (var c in next.Children) { open.Push(c); }
            }
        }

        public async Coroutine<Result> Execute()
        {
            BehaviourTree.CurrentInstance.OnNodeTickStarted(this);

            Start();

            var result = await Tick();

            Stop();

            BehaviourTree.CurrentInstance.OnNodeTickFinished(this);

            return result;
        }

        public virtual void Terminate() { }

        public virtual void Initialize() { }

        protected virtual void Start() { }

        protected virtual void Stop() { }

        protected abstract Coroutine<Result> Tick();
    }
}