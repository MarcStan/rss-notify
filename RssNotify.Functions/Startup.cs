using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RssNotify.Functions;
using RssNotify.Services;
using RssNotify.Services.Configuration;
using RssNotify.Services.Models;
using System;
using System.Linq;

[assembly: FunctionsStartup(typeof(Startup))]
namespace RssNotify.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<FunctionConfig>()
                .Configure<IConfiguration>((funcConfig, configuration) => configuration.Bind(funcConfig));
            builder.Services.AddOptions<MatrixConfiguration>()
                .Configure<IConfiguration>((matrix, configuration) =>
                {
                    configuration.Bind("Matrix", matrix);
                    // otherwise will spam with all old content
                    // on first launch this will still spam but then persist in table and on subsequent runs only new subscriptions are added
                    matrix.IgnoreSubscriptionsOlderThan = DateTimeOffset.UtcNow.Date.AddDays(-2);
                });
            builder.Services.AddSingleton(p =>
            {
                var configuration = p.GetRequiredService<IConfiguration>();
                var data = configuration.GetSection("Subscriptions").Get<string[]>();
                var parsed = data
                    .Select(JsonConvert.DeserializeObject<Subscription>)
                    .ToArray();
                return parsed;
            });

            builder.Services.AddSingleton(p =>
            {
                var connectionString = p.GetRequiredService<IOptions<FunctionConfig>>().Value.AzureWebJobsStorage;
                return CloudStorageAccount.Parse(connectionString);
            });
            // singleton to prevent connection exhaustion
            builder.Services.AddSingleton<IHttpClient, CustomHttpClient>();
            builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
            builder.Services.AddSingleton<IMatrixNotificationService, MatrixNotificationService>();
        }
    }
}
