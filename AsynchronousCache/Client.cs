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

        public void ManyCalls(IEnumerable<HttpDescriptorRequest> httpRequestList)
        {
            using (var cts = new CancellationTokenSource(25 * 1000))
            {
                var tasks = httpRequestList.Select(GetUrlAsyncOnAnotherThread);
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
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        public async Task ItIsAlive(CancellationToken cancellationToken)
        {
            _logger.DebugLog(".");
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.DebugLog(".");
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ItIsAlive(int timeoutInMilliseconds)
        {
            var endDateTime = DateTime.Now.AddMilliseconds(timeoutInMilliseconds);
            while (endDateTime > DateTime.Now)
            {
                _logger.DebugLog(".");
                await Task.Delay(250).ConfigureAwait(false);
            }
        }

        private Task<HttpDescriptorResponse> GetUrlAsyncOnAnotherThread(HttpDescriptorRequest httpRequest)
        {
            return Task.Run(() => GetUrlAsync(httpRequest));
        }

        private async Task<HttpDescriptorResponse> GetUrlAsync(HttpDescriptorRequest httpRequest)
        {
            Measures.GetAttempt++;
            _logger.DebugLog($"GetUrl() for {httpRequest.RequestUri}");

            try
            {
                if (!await httpRequest.SemaphoreSlim.WaitAsync(25 * 1000, httpRequest.CancellationToken))
                {
                    _logger.DebugLog($"Timeout for url '{httpRequest.RequestUri}' ");
                    return default(HttpDescriptorResponse);
                }
                else
                {
                    _logger.DebugLog($"Http request started for url '{httpRequest}' ");
                    return await AsyncCacheWithTimeStamp[httpRequest];
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorLog(ex);
            }
            finally
            {
                httpRequest.SemaphoreSlim.Release();
            }
            _logger.DebugLog($"Http request ended for url '{httpRequest}' ");
            return default(HttpDescriptorResponse);
        }
    }
}
