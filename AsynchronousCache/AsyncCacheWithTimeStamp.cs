using System;
using System.Threading.Tasks;

namespace AsynchronousCache
{
    public class AsyncCacheWithTimeStamp
    {
        public AsyncCache<HttpDescriptorRequest, HttpDescriptorResponse> HttpCache;

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public AsyncCacheWithTimeStamp(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            HttpCache = new AsyncCache<HttpDescriptorRequest, HttpDescriptorResponse>(HttpGetAsync);
        }

        public int Count => HttpCache.Count;

        public Task<HttpDescriptorResponse> this[HttpDescriptorRequest key] => GetValue(key);

        private async Task<HttpDescriptorResponse> GetValue(HttpDescriptorRequest key)
        {
            var result = await HttpCache[key];

            if (result != null && result.StorageTime.AddSeconds(key.CacheDurationInSeconds) > DateTime.Now)
            {
                return result;
            }

            if (!HttpCache.Remove(key))
            {
                _logger.ErrorLog($"key cannot be removed because it doesn't exists");
            }

            return await HttpCache[key];
        }

        private async Task<HttpDescriptorResponse> HttpGetAsync(HttpDescriptorRequest httpDescriptorRequest)
        {
            try
            {
                var httpRequestMessage = httpDescriptorRequest.HttpRequestMessage;
                _logger.DebugLog($"HttpGetAsync for { httpRequestMessage.RequestUri }");

                string json;

                using (var httpResponse = await _httpClient.SendAsync(httpRequestMessage, httpDescriptorRequest.CancellationToken))
                {
                    if (httpResponse?.Content == null)
                        return default(HttpDescriptorResponse);
                    if (!httpResponse.IsSuccessStatusCode)
                        return default(HttpDescriptorResponse); 

                    json = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(json))
                        return default(HttpDescriptorResponse);
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
    }
}
