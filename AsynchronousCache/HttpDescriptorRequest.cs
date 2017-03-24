using System.Net.Http;
using System.Threading;

namespace AsynchronousCache
{
    public class HttpDescriptorRequest : HttpDescriptorBase
    {
        public readonly HttpMethod HttpMethod;
        public readonly string RequestUri;
        public readonly int CacheDurationInSeconds;

        public CancellationToken CancellationToken;
        public HttpRequestMessage HttpRequestMessage { get; private set; }

        public HttpDescriptorRequest(HttpMethod httpMethod, string requestUri, int cacheDurationInSeconds)
        {
            HttpMethod = httpMethod;
            RequestUri = requestUri;
            CacheDurationInSeconds = cacheDurationInSeconds;

            CreateHttpRequestMessage();
        }

        public override string ToString()
        {
            return HttpMethod + " " + RequestUri;
        }

        public void CreateHttpRequestMessage()
        {
            HttpRequestMessage = new HttpRequestMessage(HttpMethod, RequestUri);
        }
    }
}