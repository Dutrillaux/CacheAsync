using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace AsynchronousCache.Tests
{
    [TestFixture]
    public class AsyncCacheTests
    {
        private const int TwoSecondsHttpDescriptorCacheDurationInSecond = 1;
        private const int HundredSecondsHttpDescriptorCacheDurationInSecond = 100;

        private readonly HttpDescriptorRequest _httpDescriptorRequestWithTwoSecondsCacheDuration =
            new HttpDescriptorRequest(HttpMethod.Get, "https://www.google.fr/",
                TwoSecondsHttpDescriptorCacheDurationInSecond);

        private readonly HttpDescriptorRequest _httpDescriptorRequestWithHundredSecondsCacheDuration = new HttpDescriptorRequest
            (HttpMethod.Get,  "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx",
                HundredSecondsHttpDescriptorCacheDurationInSecond);

        private Mock<IHttpClient> _httpClientMock;
        private Mock<ILogger> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _httpClientMock = new Mock<IHttpClient>();

            _httpClientMock.Setup(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()))
                .Returns(ReturnValue);
            _httpClientMock.Setup(x => x.SendAsync(_httpDescriptorRequestWithHundredSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()))
                .Returns(ReturnValue);

            _loggerMock = new Mock<ILogger>();
        }

        private static Task<HttpResponseMessage> ReturnValue()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("myJsoncontent")
            };

            return Task.FromResult(response);
        }

        [Test]
        public void One_Hit_Missed_One_Item_Stored()
        {
            var httpRequestList = new List<HttpDescriptorRequest>
            {
                _httpDescriptorRequestWithTwoSecondsCacheDuration
            };
            
            var client = new Client(_loggerMock.Object, _httpClientMock.Object);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();
            client.ManyCalls(httpRequestList);
            //_httpClientMock.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 1);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task One_Item_Stored_And_Removed_After_Cache_Duration()
        {
            var httpRequestList = new List<HttpDescriptorRequest>
            {
                _httpDescriptorRequestWithTwoSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);

            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 1);

            await Task.Delay(_httpDescriptorRequestWithTwoSecondsCacheDuration.CacheDurationInSeconds * 1000 + 10);

            _httpClientMock.ResetCalls();
            client.ManyCalls(httpRequestList);

            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task First_Calls_Then_Waiting_Over_TTL_Then_Calls_again()
        {
            var httpRequestList = new List<HttpDescriptorRequest>
            {
                _httpDescriptorRequestWithTwoSecondsCacheDuration,
                _httpDescriptorRequestWithHundredSecondsCacheDuration,
                _httpDescriptorRequestWithTwoSecondsCacheDuration,
                _httpDescriptorRequestWithHundredSecondsCacheDuration,
                _httpDescriptorRequestWithHundredSecondsCacheDuration,
                _httpDescriptorRequestWithHundredSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);
            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithHundredSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 2);

            var minCacheDurationInSeconds = Math.Min(
                _httpDescriptorRequestWithTwoSecondsCacheDuration.CacheDurationInSeconds, 
                _httpDescriptorRequestWithHundredSecondsCacheDuration.CacheDurationInSeconds);

            await Task.Delay(minCacheDurationInSeconds * 1000 + 10);

            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);
            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithTwoSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Once);
            _httpClientMock.Verify(x => x.SendAsync(_httpDescriptorRequestWithHundredSecondsCacheDuration.HttpRequestMessage, It.IsAny<CancellationToken>()), Times.Never);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 2);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }
    }
}
