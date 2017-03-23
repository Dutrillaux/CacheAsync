using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

        private readonly HttpDescriptor _httpDescriptorWithTwoSecondsCacheDuration = new HttpDescriptor
        {
            CacheDurationInSeconds = TwoSecondsHttpDescriptorCacheDurationInSecond,
            Url = "https://www.google.fr/"
        };

        private readonly HttpDescriptor _httpDescriptorWithHundredSecondsCacheDuration = new HttpDescriptor
        {
            CacheDurationInSeconds = HundredSecondsHttpDescriptorCacheDurationInSecond,
            Url = "https://msdn.microsoft.com/fr-fr/library/system.threading.tasks.dataflow(v=vs.110).aspx"
        };

        private Mock<IHttpClient> _httpClientMock;
        private Mock<ILogger> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _httpClientMock = new Mock<IHttpClient>();

            _httpClientMock.Setup(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url))
                .Returns(ReturnValue);
            _httpClientMock.Setup(x => x.GetAsync(_httpDescriptorWithHundredSecondsCacheDuration.Url))
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
            var httpRequestList = new List<HttpDescriptor>
            {
                _httpDescriptorWithTwoSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();
            client.ManyCalls(httpRequestList);
            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url), Times.Once);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 1);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task One_Item_Stored_And_Removed_After_Cache_Duration()
        {
            var httpRequestList = new List<HttpDescriptor>
            {
                _httpDescriptorWithTwoSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);

            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url), Times.Once);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 1);

            await Task.Delay(_httpDescriptorWithTwoSecondsCacheDuration.CacheDurationInSeconds * 1000 + 10);

            _httpClientMock.ResetCalls();
            client.ManyCalls(httpRequestList);

            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url), Times.Once);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }

        [Test]
        public async Task First_Calls_Then_Waiting_Over_TTL_Then_Calls_again()
        {
            var httpRequestList = new List<HttpDescriptor>
            {
                _httpDescriptorWithTwoSecondsCacheDuration,
                _httpDescriptorWithHundredSecondsCacheDuration,
                _httpDescriptorWithTwoSecondsCacheDuration,
                _httpDescriptorWithHundredSecondsCacheDuration,
                _httpDescriptorWithHundredSecondsCacheDuration,
                _httpDescriptorWithHundredSecondsCacheDuration
            };

            var client = new Client(_loggerMock.Object, _httpClientMock.Object);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 0);
            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);
            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url), Times.Once);
            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithHundredSecondsCacheDuration.Url), Times.Once);

            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 2);

            var minCacheDurationInSeconds = Math.Min(
                _httpDescriptorWithTwoSecondsCacheDuration.CacheDurationInSeconds, 
                _httpDescriptorWithHundredSecondsCacheDuration.CacheDurationInSeconds);

            await Task.Delay(minCacheDurationInSeconds * 1000 + 10);

            _httpClientMock.ResetCalls();

            client.ManyCalls(httpRequestList);
            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithTwoSecondsCacheDuration.Url), Times.Once);
            _httpClientMock.Verify(x => x.GetAsync(_httpDescriptorWithHundredSecondsCacheDuration.Url), Times.Never);
            Assert.IsTrue(client.AsyncCacheWithTimeStamp.Count == 2);

            _loggerMock.Verify(x => x.ErrorLog(It.IsAny<Exception>()), Times.Never());
        }
    }
}
