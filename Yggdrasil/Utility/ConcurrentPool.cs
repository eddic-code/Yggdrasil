﻿#region License

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

using System.Collections.Concurrent;

namespace Yggdrasil.Utility
{
    // Trying to avoid having to use locks. The pool size can still go above maximum
    // from race conditions.

    public class ConcurrentPool<T> where T : class, new()
    {
        public volatile int MaxPoolCount = int.MaxValue - 100;

        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public int Count => _queue.Count;

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }

        public void PrePool(int count)
        {
            while (count-- > 0 && _queue.Count < MaxPoolCount)
            {
                _queue.Enqueue(new T());
            }
        }

        public T Get()
        {
            if (_queue.TryDequeue(out var item)) { return item; }

            return new T();
        }

        public void Recycle(T item)
        {
            if (_queue.Count >= MaxPoolCount) { return; }
            _queue.Enqueue(item);
        }
    }
}