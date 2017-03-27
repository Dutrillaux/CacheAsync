using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AsynchronousCache
{
    public class CacheAsync<TKey, TValue>
    {
        private readonly Func<TKey, Task<TValue>> _valueFactory;
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map;

        public int Count => _map?.Count ?? 0;

        public CacheAsync(Func<TKey, Task<TValue>> valueFactory)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("loader");

            _valueFactory = valueFactory;
            _map = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
        }

        public Task<TValue> this[TKey key]
        {
            get
            {
                if (key == null)
                    return Task.FromResult(default(TValue)); // throw new ArgumentNullException("key");

                //_logger.Log($"try to Hit for {key}");
                return _map.GetOrAdd(key, ValueFactory).Value;
            }
        }

        private Lazy<Task<TValue>> ValueFactory(TKey toAdd)
        {
            //_logger.Log($"Hit missed for {toAdd}");
            Measures.HitMissed++;
            return new Lazy<Task<TValue>>(() => _valueFactory(toAdd));
        }

        public bool Remove(TKey key)
        {
            Lazy<Task<TValue>> value;
            Measures.RemovedItem++;
            //_logger.Log($"Remove for {key}");
            return _map.TryRemove(key, out value);
        }
    }
}