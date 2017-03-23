using System;
using System.Net.Http;

namespace AsynchronousCache
{
    public class HttpDescriptorResponse
    {
        public string Json;
        public HttpResponseMessage HttpResponseMessage;
        public DateTime StorageTime;
    }
}