using System;
using System.Collections.Concurrent;

namespace Yggdrasil.Utility
{
    internal class ConcurrentPoolMap<T>
    {
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<T>> _map = new ConcurrentDictionary<Type, ConcurrentQueue<T>>();
        private static readonly Func<Type, ConcurrentQueue<T>> _addFunction = s => new ConcurrentQueue<T>();

        public TR Get<TR>() where TR : class, T, new()
        {
            var stack = _map.GetOrAdd(typeof(TR), _addFunction);

            if (stack.TryDequeue(out var item)) { return item as TR; }

            return new TR();
        }

        public void Recycle(T item)
        {
            var stack = _map.GetOrAdd(item.GetType(), _addFunction);
            stack.Enqueue(item);
        }
    }
}
