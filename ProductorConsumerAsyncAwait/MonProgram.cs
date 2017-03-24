using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AsynchronousCache;

namespace ProductorConsumerAsyncAwait
{
    public class MonProgram
    {
        private readonly HttpDescriptorRequest _googleHttpDescriptorRequest = new HttpDescriptorRequest(HttpMethod.Get,
            "https://www.google.fr/", 100);

        private readonly HttpDescriptorRequest _msdnHttpDescriptorRequest = new HttpDescriptorRequest(HttpMethod.Get,
            "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx", 1);

        public async Task Start()
        {
            var httpRequestList = new List<HttpDescriptorRequest>
            {
                _googleHttpDescriptorRequest,
                _msdnHttpDescriptorRequest,
                _googleHttpDescriptorRequest,
                _msdnHttpDescriptorRequest,
                _msdnHttpDescriptorRequest,
                _msdnHttpDescriptorRequest,
            };

            var client = new Client(new Logger(), new HttpClientWrapper());

            Console.WriteLine("First group");
            client.ManyCalls(httpRequestList);

            PrintStatus();
            Console.WriteLine("Waiting");

            var minduration = Math.Min(_googleHttpDescriptorRequest.CacheDurationInSeconds,
                _msdnHttpDescriptorRequest.CacheDurationInSeconds);

            await client.ItIsAlive(minduration * 1000 + 10);

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