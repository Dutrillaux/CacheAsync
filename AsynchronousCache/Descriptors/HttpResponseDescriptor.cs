using System;
using System.Net.Http;

namespace AsynchronousCache.Descriptors
{
    public class HttpResponseDescriptor
    {
        public string Json;
        public HttpResponseMessage HttpResponseMessage;
        public DateTime StorageTime;
    }
}