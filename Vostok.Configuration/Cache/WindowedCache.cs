using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Configuration.Cache
{
    internal class WindowedCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Action<KeyValuePair<TKey, TValue>> onAutoRemove;
        private readonly ConcurrentDictionary<TKey, TValue> cache = new ConcurrentDictionary<TKey, TValue>();
        private readonly ConcurrentQueue<TKey> queue = new ConcurrentQueue<TKey>();

        public WindowedCache(int capacity, Action<KeyValuePair<TKey, TValue>> onAutoRemove)
        {
            this.capacity = capacity;
            this.onAutoRemove = onAutoRemove;
        }

        public IEnumerable<TValue> Values => cache.Select(pair => pair.Value);

        public bool TryGetValue(TKey key, out TValue value) => cache.TryGetValue(key, out value);

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (!cache.ContainsKey(key))
                queue.Enqueue(key);

            var value = cache.GetOrAdd(key, _ => valueFactory(key));

            RemoveOutOfWindowItems();

            return value;
        }

        public bool TryRemove(TKey key, out TValue value) => cache.TryRemove(key, out value);

        private void RemoveOutOfWindowItems()
        {
            while (queue.Count > capacity && queue.TryDequeue(out var keyToRemove))
                if (cache.TryRemove(keyToRemove, out var removedValue))
                    onAutoRemove(new KeyValuePair<TKey, TValue>(keyToRemove, removedValue));
        }
    }
}