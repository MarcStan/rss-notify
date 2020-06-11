using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public class CustomHttpClient : IHttpClient
    {
        private readonly HttpClient _client = new HttpClient();

        public Task<HttpResponseMessage> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _client.SendAsync(request, cancellationToken);

        public Task<HttpResponseMessage> PutAsync<T>(string url, T json, CancellationToken cancellationToken)
            => _client.PutAsync(url, new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json"), cancellationToken);
    }
}
