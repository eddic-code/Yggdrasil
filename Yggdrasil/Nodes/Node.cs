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

using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public abstract class Node
    {
        protected readonly CoroutineManager Manager;

        protected Node(CoroutineManager manager)
        {
            Manager = manager;
        }

        protected Coroutine Yield => Manager.Yield;
        protected Coroutine<Result> Success => Coroutine<Result>.CreateWith(Result.Success);
        protected Coroutine<Result> Failure => Coroutine<Result>.CreateWith(Result.Failure);
        protected object State => Manager.State;

        public string Guid { get; set; }

        public async Coroutine<Result> Execute()
        {
            Manager.OnNodeTickStarted(this);

            Start();

            var result = await Tick();

            Stop();

            Manager.OnNodeTickFinished(this);

            return result;
        }

        public virtual void Terminate() { }

        protected virtual void Start() { }
        protected virtual void Stop() { }
        protected abstract Coroutine<Result> Tick();
    }
}