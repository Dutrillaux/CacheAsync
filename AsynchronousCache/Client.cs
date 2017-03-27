using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsynchronousCache.Descriptors;
using AsynchronousCache.HttpClient;

namespace AsynchronousCache
{
    public class Client
    {
        private readonly ILogger _logger;
        public readonly TimeStampedCacheAsync TimeStampedCacheAsync;

        public Client(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            TimeStampedCacheAsync = new TimeStampedCacheAsync(httpClient, _logger);
        }

        public async Task ProceedManySimultaneaousCalls(IEnumerable<HttpBaseRequestDescriptor> httpRequestList)
        {
            var tasks = httpRequestList.Select(GetHttpResponseOnAnotherThreadAsync);
            await Task.WhenAll(tasks);
        }

        private Task<HttpResponseDescriptor> GetHttpResponseOnAnotherThreadAsync(HttpBaseRequestDescriptor httpBaseRequest)
        {
            return Task.Run(() => GetHttpResponseAsync(httpBaseRequest));
        }

        private async Task<HttpResponseDescriptor> GetHttpResponseAsync(HttpBaseRequestDescriptor httpBaseRequest)
        {
            Measures.GetAttempt++;
            _logger.DebugLog($"GetHttpResponseAsync() for {httpBaseRequest.RequestUri}");

            try
            {
                if (!await httpBaseRequest.SemaphoreSlim.WaitAsync(25 * 1000, httpBaseRequest.CancellationToken))
                {
                    _logger.DebugLog($"Timeout on GetHttpResponseAsync for '{httpBaseRequest.RequestUri}' ");
                    return default(HttpResponseDescriptor);
                }
                else
                {
                    _logger.DebugLog($"Http GetHttpResponseAsync started for url '{ httpBaseRequest.RequestUri }' ");
                    return await TimeStampedCacheAsync[httpBaseRequest];
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex);
            }
            finally
            {
                if (httpBaseRequest.SemaphoreSlim.CurrentCount < HttpBaseDescriptor.SemaphoreSlimMaxCount)
                    httpBaseRequest.SemaphoreSlim.Release();
            }
            _logger.DebugLog($"Http GetHttpResponseAsync ended for url '{ httpBaseRequest.RequestUri }' ");

            return default(HttpResponseDescriptor);
        }
    }
}
