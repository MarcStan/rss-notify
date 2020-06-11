using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using RssNotify.Services;
using RssNotify.Services.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Functions
{
    public class BotFunctions
    {
        private const string Every5Minutes = "0 0/5 * * * *";
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMatrixNotificationService _notificationService;
        private readonly MatrixConfiguration _configuration;

        public BotFunctions(
            ISubscriptionService subscriptionService,
            IMatrixNotificationService notificationService,
            IOptions<MatrixConfiguration> options)
        {
            _subscriptionService = subscriptionService;
            _notificationService = notificationService;
            _configuration = options.Value;
        }

        /// <summary>
        /// Periodically checks for new subscription and posts them if necessary.
        /// </summary>
        [FunctionName("bot-check")]
        public async Task CheckAsync(
            [TimerTrigger(Every5Minutes, RunOnStartup = true)] TimerInfo timer,
            CancellationToken cancellationToken)
        {
            var updates = await _subscriptionService.GetLatestAsync(cancellationToken);
            foreach (var u in updates.OrderBy(x => x.LastUpdated))
            {
                var date = TimeZoneInfo
                    .ConvertTimeFromUtc(u.LastUpdated.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById(_configuration.TimeZone))
                    .ToString("yyyy/MM/dd HH:mm");
                await _notificationService.SendAsync($"{u.Name} has posted <a href=\"{u.Url}\">{u.Message}</a> @ {date}", cancellationToken);
                await _subscriptionService.MarkAsReceivedAsync(u, cancellationToken);
            }
        }

        /// <summary>
        /// Test endpoint that allows posting as the bot.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        [FunctionName("bot-post")]
        public async Task<IActionResult> PostAsync(
            [HttpTrigger("POST", Route = "message")] Message message,
            CancellationToken cancellationToken)
        {
            if (message == null ||
                string.IsNullOrEmpty(message.Subject) ||
                string.IsNullOrEmpty(message.Body))
            {
                throw new NotSupportedException("Expected message to contain subject and body!");
            }

            await _notificationService.SendAsync($"{message.Subject}: {message.Body}", cancellationToken);

            return new OkResult();
        }

        public class Message
        {
            public string Subject { get; set; }

            public string Body { get; set; }
        }
    }
}
