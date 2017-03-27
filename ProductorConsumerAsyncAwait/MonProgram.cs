using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AsynchronousCache;
using AsynchronousCache.Descriptors;
using AsynchronousCache.HttpClient;

namespace ProductorConsumerAsyncAwait
{
    public class MonProgram
    {
        private readonly HttpBaseRequestDescriptor _googleHttpBaseRequestDescriptor = new HttpBaseRequestDescriptor(HttpMethod.Get,
            "https://www.google.fr/", 100 * 1000);

        private readonly HttpBaseRequestDescriptor _msdnHttpBaseRequestDescriptor = new HttpBaseRequestDescriptor(HttpMethod.Get,
            "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx", 1 * 1000);

        public async Task Start()
        {
            var httpRequestList = new List<HttpBaseRequestDescriptor>
            {
                _googleHttpBaseRequestDescriptor,
                _msdnHttpBaseRequestDescriptor,
                _googleHttpBaseRequestDescriptor,
                _msdnHttpBaseRequestDescriptor,
                _msdnHttpBaseRequestDescriptor,
                _msdnHttpBaseRequestDescriptor,
            };

            var client = new Client(new Logger(), new HttpClientWrapper());

            Console.WriteLine("Starting ...");
            Console.WriteLine("> Calling a first time the descriptor list ");
            await client.ProceedManySimultaneaousCalls(httpRequestList);

            PrintStatus();

            var minDurationInMilliSeconds = Math.Min(_googleHttpBaseRequestDescriptor.CacheDurationInMilliSeconds,
                _msdnHttpBaseRequestDescriptor.CacheDurationInMilliSeconds);
            var waitingMilliseconds = minDurationInMilliSeconds + 10;
            Console.WriteLine($"  Lesser cache duration : { minDurationInMilliSeconds } milliseconds");
            Console.WriteLine($"  Waiting for { waitingMilliseconds } milliseconds");
            await Task.Delay(waitingMilliseconds);

            Console.WriteLine("");
            Console.WriteLine("> Calling a second time the descriptor list ");
            await client.ProceedManySimultaneaousCalls(httpRequestList);

            PrintStatus();
            Console.WriteLine("end");
        }

        private static void PrintStatus()
        {
            Console.WriteLine("");
            Console.WriteLine(" ---- Overall status ----");
            Console.WriteLine("");
            Console.WriteLine("| Gets  | Cache hits missed | Cache hit |");
            Console.WriteLine(
                $"|   {Measures.GetAttempt.ToString("00")}  |        {Measures.HitMissed.ToString("00")}         |     {(Measures.GetAttempt - Measures.HitMissed).ToString("00")}    |");
            Console.WriteLine("");
        }
    }
}