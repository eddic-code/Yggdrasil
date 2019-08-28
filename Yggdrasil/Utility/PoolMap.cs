﻿#region License

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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Yggdrasil.Utility
{
    internal class PoolMap<T>
    {
        private static readonly Func<Type, Stack<T>> _addFunction = s => new Stack<T>(100);
        private readonly ConcurrentDictionary<Type, Stack<T>> _map = new ConcurrentDictionary<Type, Stack<T>>();

        public TR Get<TR>() where TR : class, T, new()
        {
            var stack = _map.GetOrAdd(typeof(TR), _addFunction);

            if (stack.Count > 0) { return stack.Pop() as TR; }

            return new TR();
        }

        public void Recycle(T item)
        {
            var stack = _map.GetOrAdd(item.GetType(), _addFunction);
            stack.Push(item);
        }
    }
}