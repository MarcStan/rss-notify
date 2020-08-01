using Microsoft.Extensions.Options;
using RssNotify.Services.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public class MatrixNotificationService : IMatrixNotificationService
    {
        private readonly IHttpClient _httpClient;
        private readonly MatrixConfiguration _configuration;
        private const string BaseUrl = "https://matrix.org/_matrix";

        public MatrixNotificationService(
            IHttpClient httpClient,
            IOptions<MatrixConfiguration> options)
        {
            _httpClient = httpClient;
            _configuration = options.Value;
        }

        public Task SendAsync(string message, CancellationToken cancellationToken)
            => SendAsync(message, null, cancellationToken);

        public async Task SendAsync(string message, string roomId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(roomId))
                roomId = null;

            var reqId = Guid.NewGuid().ToString();
            var response = await _httpClient.PutAsync($"{BaseUrl}/client/r0/rooms/{roomId ?? _configuration.RoomId}/send/m.room.message/{reqId}?access_token={_configuration.AccessToken}", new
            {
                // https://github.com/matrix-org/matrix-doc/pull/1397/files
                msgtype = "m.text",
                // not optional. must be set to empty or message is not delivered!
                body = "",
                formatted_body = message,
                format = "org.matrix.custom.html"
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new ResponseException($"Failed to deliver notification {message} to matrix. Response: ({response.StatusCode}) {await response.Content.ReadAsStringAsync()}");
        }
    }
}
