using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsynchronousCache;

namespace ProductorConsumerAsyncAwait
{
    public class MonProgram
    {
        private HttpDescriptor GoogleHttpDescriptor = new HttpDescriptor
        {
            CacheDurationInSeconds = 100,
            Url = "https://www.google.fr/"
        };

        private HttpDescriptor MsdnHttpDescriptor = new HttpDescriptor
        {
            CacheDurationInSeconds = 1,
            Url = "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx"
        };

        public async Task Start()
        {
            var httpRequestList = new List<HttpDescriptor>
            {
                GoogleHttpDescriptor,
                MsdnHttpDescriptor,
                GoogleHttpDescriptor,
                MsdnHttpDescriptor,
                MsdnHttpDescriptor,
                MsdnHttpDescriptor,
            };

            // var httpRequester = new HttpRequester();
            //var applicationRequests = new ApplicationRequests();
            //var cacheUsage = new AsyncCacheWithTimeStamp(new HttpClientWrapper());
            var client  = new Client(new Logger(), new HttpClientWrapper());

            Console.WriteLine("First group");
            client.ManyCalls(httpRequestList);

            PrintStatus();
            Console.WriteLine("Wait for 10 sec");

            using (var cts = new CancellationTokenSource(25 * 1000))
            {
                var waitingTask = client.ItIsAlive(cts.Token);
                Task.WaitAny(Task.Delay(10 * 1000), waitingTask);
                cts.Cancel();
            }

            Console.WriteLine("Second group");
            client.ManyCalls(httpRequestList);

            Console.WriteLine("end");
            PrintStatus();
        }

        private static void PrintStatus()
        {
            Console.WriteLine("| Gets  | Cache hits missed | Cache hit |");
            Console.WriteLine(
                $"|   {Measures.GetAttempt.ToString("00")}  |        {Measures.HitMissed.ToString("00")}         |     {(Measures.GetAttempt - Measures.HitMissed).ToString("00")}    |");
            Console.WriteLine();
        }
    }
}