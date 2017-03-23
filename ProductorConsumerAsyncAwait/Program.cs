using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProductorConsumerAsyncAwait
{
    class Program
    {
        static void Main(string[] args)
        {
            var monProgram = new MonProgram();
            Task.WaitAll(monProgram.Start());
            Console.ReadLine();
        }
    }

    public class HttpDescriptorResponse
    {
        public HttpResponseMessage HttpResponseMessage;
        public DateTime StorageTime;
    }
}
