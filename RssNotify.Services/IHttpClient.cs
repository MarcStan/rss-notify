using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PutAsync<T>(string url, T json, CancellationToken cancellationToken);
    }
}

