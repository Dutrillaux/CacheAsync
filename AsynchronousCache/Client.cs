using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsynchronousCache
{
    public class Client
    {
        private readonly ILogger _logger;
        public readonly AsyncCacheWithTimeStamp AsyncCacheWithTimeStamp;

        public Client(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            AsyncCacheWithTimeStamp = new AsyncCacheWithTimeStamp(httpClient, _logger);
        }

        public void ManyCalls(IEnumerable<HttpDescriptor> httpRequestList)
        {
            using (var cts = new CancellationTokenSource(25 * 1000))
            {
                var tasks = httpRequestList.Select(method => GetUrlAsyncOnAnotherThread(method));
                var waitingTask = ItIsAlive(cts.Token);
                Task.WaitAny(Task.WhenAll(tasks), waitingTask);
                cts.Cancel();
            }
        }

        public async Task ItIsAlive()
        {
            _logger.DebugLog(".");
            while (true)
            {
                _logger.DebugLog(".");
                await Task.Delay(500);
            }
        }

        public async Task ItIsAlive(CancellationToken token)
        {
            _logger.DebugLog(".");
            while (!token.IsCancellationRequested)
            {
                _logger.DebugLog(".");
                await Task.Delay(500);
            }
        }

        private Task<HttpDescriptorResponse> GetUrlAsyncOnAnotherThread(HttpDescriptor httpRequest)
        {
            return Task.Run(() => GetUrlAsync(httpRequest));
        }

        private async Task<HttpDescriptorResponse> GetUrlAsync(HttpDescriptor httpRequest)
        {
            //_logger.DebugLog($"GetUrl() for {httpRequest.Url}");
            Measures.GetAttempt++;
            try
            {
                if (!await httpRequest.SemaphoreSlim.WaitAsync(25 * 1000))
                {
                    //_logger.DebugLog($"Timeout for url '{httpRequest.Url}' ");
                    return default(HttpDescriptorResponse);
                }

                //_logger.DebugLog($"Http request started for url '{httpRequest}' ");
                return await AsyncCacheWithTimeStamp[httpRequest];
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex);
            }
            finally
            {
                // if (httpRequest.SemaphoreSlim.CurrentCount > 0)
                httpRequest.SemaphoreSlim.Release();
            }
           // _logger.DebugLog($"Http request ended for url '{httpRequest}' ");
            return default(HttpDescriptorResponse);
        }
    }
}
