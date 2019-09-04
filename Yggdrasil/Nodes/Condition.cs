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

using System;
using System.Xml.Serialization;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Nodes
{
    public class Condition : Node
    {
        public Condition(CoroutineManager manager, Func<object, bool> conditional) : base(manager)
        {
            Conditional = conditional;
        }

        public Condition(CoroutineManager manager) : base(manager) { }

        [XmlIgnore]
        public Func<object, bool> Conditional { get; set; } = DefaultConditional;

        protected override Coroutine<Result> Tick()
        {
            return Conditional(State) ? Success : Failure;
        }

        private static bool DefaultConditional(object s)
        {
            return true;
        }
    }
}