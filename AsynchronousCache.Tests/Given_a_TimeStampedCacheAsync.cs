using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsynchronousCache.Descriptors;
using AsynchronousCache.HttpClient;
using Moq;
using NUnit.Framework;

namespace AsynchronousCache.Tests
{
    [TestFixture]
    public class Given_a_TimeStampedCacheAsync
    {
        private const int Two = 2;
        private const int OneHundred = 100;

        internal sealed class TwoSecondsHttpRequestDescriptor : HttpBaseRequestDescriptor
        {
            public TwoSecondsHttpRequestDescriptor()
                : base(HttpMethod.Get, "https://www.2seconds.fr/", Two * 1000)
            {
                HttpRequestMessage = CreateHttpRequestMessage();
            }

            public override HttpRequestMessage CreateHttpRequestMessage()
            {
                HttpRequestMessage = new TwoSecondsHttpRequestMessage(HttpMethod, RequestUri);
                return HttpRequestMessage;
            }

            public class TwoSecondsHttpRequestMessage : HttpRequestMessage
            {
                public TwoSecondsHttpRequestMessage(HttpMethod method, string requestUri) : base(method, requestUri)
                {
                }
            }

            public class TwoSecondsHttpResponseMessage : HttpResponseMessage
            {
            }
        }

        internal sealed class OneHundredSecondsHttpRequestDescriptor : HttpBaseRequestDescriptor
        {
            public OneHundredSecondsHttpRequestDescriptor()
                : base(HttpMethod.Get, "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx", OneHundred * 1000)
            {
                HttpRequestMessage = CreateHttpRequestMessage();
            }

            public override HttpRequestMessage CreateHttpRequestMessage()
            {
                HttpRequestMessage = new OneHundredSecondsHttpRequestMessage(HttpMethod, RequestUri);
                return HttpRequestMessage;
            }

            public class OneHundredSecondsHttpRequestMessage : HttpRequestMessage
            {
                public OneHundredSecondsHttpRequestMessage(HttpMethod method, string requestUri) : base(method, requestUri)
                {
                }
            }

            public class OneHundredSecondsHttpResponseMessage : HttpResponseMessage
            {
            }
        }

        private Mock<IHttpClient> _httpClientMock;
        private Mock<ILogger> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _httpClientMock = new Mock<IHttpClient>();

            _httpClientMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns((HttpRequestMessage x, CancellationToken ct) => ReturnTypedHttpResponse(x));

            _loggerMock = new Mock<ILogger>();
        }

        private static async Task<HttpResponseMessage> ReturnTypedHttpResponse(HttpRequestMessage request)
        {
            if (request is TwoSecondsHttpRequestDescriptor.TwoSecondsHttpRequestMessage)
            {
                return await GetTwoSecondsHttpRequestMessage();
            }
            if (request is OneHundredSecondsHttpRequestDescriptor.OneHundredSecondsHttpRequestMessage)
            {
                return await GetOneHundredSecondsHttpRequestMessage();
            }

            return default(HttpResponseMessage);
        }

        private static Task<HttpResponseMessage> GetTwoSecondsHttpRequestMessage()
        {
            var response = new TwoSecondsHttpRequestDescriptor.TwoSecondsHttpResponseMessage
            {
                Content = new StringContent("myJsoncontent for ReturnTwoHttpRequestMessage")
            };

            return Task.FromResult((HttpResponseMessage)response);
        }

        private static Task<HttpResponseMessage> GetOneHundredSecondsHttpRequestMessage()
        {
            var response = new OneHundredSecondsHttpRequestDescriptor.OneHundredSecondsHttpResponseMessage
            {
                Content = new StringContent("myJsoncontent for ReturnHundredHttpRequestMessage")
            };

            return Task.FromResult((HttpResponseMessage)response);
        }

        [Test]
        public async Task When_One_Hit_Missed_Then_One_Item_Should_Be_Stored()
        {
            var httpDescriptorRequestWithTwoSecondsCacheDuration = new TwoSecondsHttpRequestDescriptor();

            var httpRequestList = new List<HttpBaseRequestDescriptor>
            {
                httpDescriptorRequestWithTwoSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 0);
            _httpClientMock.ResetCalls();

            await client.ProceedManySimultaneaousCalls(httpRequestList);

            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 1);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task When_One_Item_Stored_And_More_Time_Than_His_Cache_Duration_Then_The_Next_Call_Thould_Be_An_Hit_Missed()
        {
            var httpDescriptorRequestWithTwoSecondsCacheDuration = new TwoSecondsHttpRequestDescriptor();

            var httpRequestList = new List<HttpBaseRequestDescriptor>
            {
                httpDescriptorRequestWithTwoSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 0);
            _httpClientMock.ResetCalls();

            await client.ProceedManySimultaneaousCalls(httpRequestList);

            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 1);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);

            await Task.Delay(httpDescriptorRequestWithTwoSecondsCacheDuration.CacheDurationInMilliSeconds + 10);

            _httpClientMock.ResetCalls();

            await client.ProceedManySimultaneaousCalls(httpRequestList);

            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 1);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task When_Many_Items_Stored_And_More_Time_Than_One_Cache_Duration_Then_The_Next_Many_Call_Should_Have_Hit_Missed_And_Hit_Ok()
        {
            var httpDescriptorRequestWithTwoSecondsCacheDuration = new TwoSecondsHttpRequestDescriptor();
            var httpDescriptorRequestWithHundredSecondsCacheDuration = new OneHundredSecondsHttpRequestDescriptor();

            var httpRequestList = new List<HttpBaseRequestDescriptor>
            {
                httpDescriptorRequestWithTwoSecondsCacheDuration,
                httpDescriptorRequestWithHundredSecondsCacheDuration,
                httpDescriptorRequestWithTwoSecondsCacheDuration,
                httpDescriptorRequestWithHundredSecondsCacheDuration,
                httpDescriptorRequestWithHundredSecondsCacheDuration,
                httpDescriptorRequestWithHundredSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 0);
            _httpClientMock.ResetCalls();

            await client.ProceedManySimultaneaousCalls(httpRequestList);

            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 2);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithHundredSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);

            var minCacheDurationInMilliSeconds = Math.Min(
                httpDescriptorRequestWithTwoSecondsCacheDuration.CacheDurationInMilliSeconds,
                httpDescriptorRequestWithHundredSecondsCacheDuration.CacheDurationInMilliSeconds);
            await Task.Delay(minCacheDurationInMilliSeconds + 10);
            _httpClientMock.ResetCalls();

            await client.ProceedManySimultaneaousCalls(httpRequestList);

            Assert.IsTrue(client.TimeStampedCacheAsync.Count == 2);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _httpClientMock.Verify(x => x.SendAsync(httpDescriptorRequestWithHundredSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Never);
            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }
    }
}
