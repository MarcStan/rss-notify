using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RssNotify.Services.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public class SubscriptionProvider : ISubscriptionProvider
    {
        private readonly IConfiguration _configuration;
        private Subscription[] _subscriptions;

        public SubscriptionProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Subscription[]> GetSubscriptionsAsync(CancellationToken cancellationToken)
        {
            // in memory caching is fine because we run on a triggered schedule
            // prevents multiple service requests per run without generating stale data
            if (_subscriptions != null)
                return _subscriptions;

            var source = _configuration["SubscriptionSource"]?.ToLowerInvariant();
            if (string.IsNullOrEmpty(source))
            {
                // originally only inline Subscriptions was supported (loaded from any valid IConfiguration source such as app settings, json, keyvault, ..)
                source = "config";
            }

            // now also custom sources are supported
            switch (source)
            {
                case "config":
                    var data = _configuration.GetSection("Subscriptions").Get<string[]>();
                    _subscriptions = data
                        .Select(JsonConvert.DeserializeObject<Subscription>)
                        .ToArray();
                    break;
                case "storage":
                    // hardcoded to config/rss-notify.json for now

                    var connectionString = _configuration["AzureWebJobsStorage"];
                    const string containerName = "config";

                    var container = new BlobContainerClient(connectionString, containerName);

                    await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                    var blob = container.GetBlockBlobClient("rss-notify.json");
                    if (!(await blob.ExistsAsync(cancellationToken)))
                        throw new NotSupportedException($"Could not find file config/rss-notify.json. It is required when using 'storage' config source");

                    using (var ms = new MemoryStream())
                    {
                        await blob.DownloadToAsync(ms, cancellationToken);
                        ms.Position = 0;
                        using (var reader = new StreamReader(ms))
                        {
                            var json = await reader.ReadToEndAsync();
                            var cfg = JsonConvert.DeserializeAnonymousType(json, new
                            {
                                subscriptions = new Subscription[0]
                            });
                            _subscriptions = cfg.subscriptions;
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported subscription source {source}");
            }
            return _subscriptions;
        }
    }
}
