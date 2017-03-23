using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AsynchronousCache
{
    public class AsyncCacheWithTimeStamp
    {
        public AsyncCache<string, HttpDescriptorResponse> HttpCache;

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public AsyncCacheWithTimeStamp(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            HttpCache = new AsyncCache<string, HttpDescriptorResponse>(HttpGetAsync);
        }

        public int Count => HttpCache.Count;

        public Task<HttpDescriptorResponse> this[HttpDescriptor key] => GetValue(key);

        private async Task<HttpDescriptorResponse> GetValue(HttpDescriptor key)
        {
            var result = await HttpCache[key.Url];

            if (result != null && result.StorageTime.AddSeconds(key.CacheDurationInSeconds) > DateTime.Now)
            {   
                return result;
            }

            if (!HttpCache.Remove(key.Url))
            {
                _logger.ErrorLog($"key cannot be removed because it doesn't exists");
                //_logger.DebugLog($"key is removed : {key.Url}");
            }

            return await HttpCache[key.Url];
        }

        private async Task<HttpDescriptorResponse> HttpGetAsync(string url)
        {
            try
            {
                Debug.WriteLine($"HttpGetAsync for{url}");

                string json;
                using (var httpResponse = await _httpClient.GetAsync(url))
                {
                    if (httpResponse?.Content == null)
                        return default(HttpDescriptorResponse);// throw new InvalidDataException("httpResponse or content null");
                    if (!httpResponse.IsSuccessStatusCode)
                        return default(HttpDescriptorResponse); //throw new InvalidDataException("httpResponse Is not SuccessStatusCode");

                    // on stock le json dans le cache 
                    json = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(json)) throw new InvalidDataException("json empty");
                }

                return new HttpDescriptorResponse
                {
                    Json = json,
                    StorageTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex);
            }

            return default(HttpDescriptorResponse);
        }

        public class AsyncCache<TKey, TValue>
        {
            private readonly Func<TKey, Task<TValue>> _valueFactory;
            private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map;

            public int Count => _map?.Count ?? 0;

            public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
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
}
