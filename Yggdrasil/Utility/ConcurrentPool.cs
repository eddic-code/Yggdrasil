using System.Collections.Concurrent;

namespace Yggdrasil.Utility
{
    internal class ConcurrentPool<T> where T: class, new()
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public T Get()
        {
            if (_queue.TryDequeue(out var item)) { return item; }

            return new T();
        }

        public void Recycle(T item)
        {
            _queue.Enqueue(item);
        }
    }
}