using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Yggdrasil.Utility
{
    internal class PoolMap<T>
    {
        private readonly ConcurrentDictionary<Type, Stack<T>> _map = new ConcurrentDictionary<Type, Stack<T>>();
        private static readonly Func<Type, Stack<T>> _addFunction = s => new Stack<T>(100);

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