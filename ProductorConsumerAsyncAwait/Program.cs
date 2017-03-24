using System;
using System.Net.Http;

namespace ProductorConsumerAsyncAwait
{
    class Program
    {
        static void Main(string[] args)
        {
            var monProgram = new MonProgram();
            monProgram.Start();
            Console.ReadLine();
        }
    }

    public class HttpDescriptorResponse
    {
        public HttpResponseMessage HttpResponseMessage;
        public DateTime StorageTime;
    }
}
