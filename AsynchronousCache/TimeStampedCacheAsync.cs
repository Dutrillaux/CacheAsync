using System;
using System.Threading.Tasks;
using AsynchronousCache.Descriptors;
using AsynchronousCache.HttpClient;

namespace AsynchronousCache
{
    public class TimeStampedCacheAsync
    {
        public CacheAsync<HttpBaseRequestDescriptor, HttpResponseDescriptor> CacheAsync;

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public TimeStampedCacheAsync(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            CacheAsync = new CacheAsync<HttpBaseRequestDescriptor, HttpResponseDescriptor>(HttpGetAsync);
        }

        public int Count => CacheAsync.Count;

        public Task<HttpResponseDescriptor> this[HttpBaseRequestDescriptor key] => GetOrAddValue(key);

        private async Task<HttpResponseDescriptor> GetOrAddValue(HttpBaseRequestDescriptor key)
        {
            var result = await CacheAsync[key];

            if (result != null)
            {
                if (result.StorageTime.AddMilliseconds(key.CacheDurationInMilliSeconds) > DateTime.Now)
                {
                    return result;
                }

                if (!CacheAsync.Remove(key))
                {
                    _logger.ErrorLog($"key cannot be removed because it doesn't exists");
                }
            }

            return await CacheAsync[key];
        }

        private async Task<HttpResponseDescriptor> HttpGetAsync(HttpBaseRequestDescriptor httpBaseRequestDescriptor)
        {
            try
            {
                var httpRequestMessage = httpBaseRequestDescriptor.CreateHttpRequestMessage();
                _logger.DebugLog($"HttpGetAsync for { httpRequestMessage.RequestUri }");

                string json;

                using (var httpResponse = await _httpClient.SendAsync(httpRequestMessage, httpBaseRequestDescriptor.CancellationToken))
                {
                    if (httpResponse?.Content == null)
                        return default(HttpResponseDescriptor);
                    if (!httpResponse.IsSuccessStatusCode)
                        return default(HttpResponseDescriptor); 

                    json = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(json))
                        return default(HttpResponseDescriptor);
                }

                return new HttpResponseDescriptor
                {
                    Json = json,
                    StorageTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex);
            }

            return default(HttpResponseDescriptor);
        }
    }
}
