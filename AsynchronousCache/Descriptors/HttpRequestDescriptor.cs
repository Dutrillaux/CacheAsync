using System.Net.Http;
using System.Threading;

namespace AsynchronousCache.Descriptors
{
    public class HttpBaseRequestDescriptor : HttpBaseDescriptor
    {
        public readonly HttpMethod HttpMethod;
        public readonly string RequestUri;
        public readonly int CacheDurationInMilliSeconds;

        public CancellationToken CancellationToken;
        public HttpRequestMessage HttpRequestMessage { get; protected set; }

        public HttpBaseRequestDescriptor(HttpMethod httpMethod, string requestUri, int cacheDurationInMilliSeconds)
        {
            HttpMethod = httpMethod;
            RequestUri = requestUri;
            CacheDurationInMilliSeconds = cacheDurationInMilliSeconds;
        }

        public override string ToString()
        {
            return HttpMethod + " " + RequestUri;
        }

        public virtual HttpRequestMessage CreateHttpRequestMessage()
        {
            HttpRequestMessage = new HttpRequestMessage(HttpMethod, RequestUri);
            return HttpRequestMessage;
        }
    }
}