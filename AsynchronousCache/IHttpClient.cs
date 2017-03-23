using System.Net.Http;
using System.Threading.Tasks;

namespace AsynchronousCache
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string requestUri);
    }
}