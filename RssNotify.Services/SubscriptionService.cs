using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using RssNotify.Services.Configuration;
using RssNotify.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace RssNotify.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IHttpClient _httpClient;
        private readonly CloudTable _table;
        private MatrixConfiguration _configuration;
        private readonly ISubscriptionProvider _subscriptionProvider;

        public SubscriptionService(
            IHttpClient httpClient,
            ISubscriptionProvider subscriptionProvider,
            CloudStorageAccount account,
            IOptions<MatrixConfiguration> configuration)
        {
            _httpClient = httpClient;

            var tableName = configuration.Value.TableName;
            _configuration = configuration.Value;
            var client = account.CreateCloudTableClient();
            _table = client.GetTableReference(tableName);
            _subscriptionProvider = subscriptionProvider;
        }

        public async Task<SubscriptionUpdate[]> GetLatestAsync(CancellationToken cancellationToken)
        {
            var subscriptions = await _subscriptionProvider.GetSubscriptionsAsync(cancellationToken);
            var tasks = subscriptions
                .Select(async s =>
                {
                    try
                    {
                        return await UpdateSubscriptionAsync(s, cancellationToken);
                    }
                    catch (Exception)
                    {
                        // TODO: will result in loss of data if not tracked
                        return await Task.FromResult(new SubscriptionUpdate[0]);
                    }
                })
                .ToList();

            await Task.WhenAll(tasks);

            return tasks
                .Where(t => t.Result != null)
                .SelectMany(t => t.Result)
                .ToArray();
        }

        public async Task MarkAsReceivedAsync(SubscriptionUpdate update, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(null, null, cancellationToken);

            var model = new ReceivedSubscriptionModel(update);

            await _table.ExecuteAsync(TableOperation.InsertOrReplace(model), null, null, cancellationToken);
        }

        /// <summary>
        /// Checks if a specific update has been received previousl.y
        /// </summary>
        /// <returns>True if already received</returns>
        private async Task<bool> HasPreviouslyBeenReceivedAsync(SubscriptionUpdate s, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(null, null, cancellationToken);

            var model = new ReceivedSubscriptionModel(s);

            var partitionMatch = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, model.PartitionKey);
            var rowMatch = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, model.RowKey);
            var pointQuery = TableQuery.CombineFilters(partitionMatch, TableOperators.And, rowMatch);
            var tableQuery = new TableQuery<ReceivedSubscriptionModel>
            {
                FilterString = pointQuery
            };

            var tableQueryResult = await _table.ExecuteQuerySegmentedAsync(tableQuery, null, null, null, cancellationToken);

            return tableQueryResult.Results.Count > 0;
        }

        private async Task<SubscriptionUpdate[]> UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            SubscriptionUpdate[] updates = null;
            // TODO: could support different types (e.g. website scrapping where no rss exists) in the future
            switch (subscription.Type)
            {
                case "rss":
                    updates = await CheckRssForUpdatesAsync(subscription, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported {subscription.Type}");
            }

            var process = updates
                .Where(u => u.LastUpdated >= _configuration.IgnoreSubscriptionsOlderThan)
                .Select(u => (update: u, task: HasPreviouslyBeenReceivedAsync(u, cancellationToken)))
                .ToArray();

            await Task.WhenAll(process.Select(t => t.task));

            return process
                .Where(u => !u.task.Result && u.update != null)
                .Select(u => u.update)
                .ToArray();
        }

        private async Task<SubscriptionUpdate[]> CheckRssForUpdatesAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            var url = subscription.Url;

            var response = await _httpClient.GetAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new ResponseException($"Failed to check for subscription update of {url}. Response code: {response.StatusCode}, Response: {content}");
            }

            var updates = new List<SubscriptionUpdate>();

            // https://github.com/dotnet/SyndicationFeedReaderWriter
            using (var xmlReader = XmlReader.Create(await response.Content.ReadAsStreamAsync(), new XmlReaderSettings() { Async = true }))
            {
                // TODO: auto detect Atom vs Rss
                var feedReader = new AtomFeedReader(xmlReader);
                while (await feedReader.Read())
                {
                    switch (feedReader.ElementType)
                    {
                        case SyndicationElementType.Item:
                            ISyndicationItem item = await feedReader.ReadItem();
                            var date = item.Published > item.LastUpdated ? item.Published : item.LastUpdated;
                            var title = item.Title ?? $"update by {subscription.Name}";
                            var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? subscription.Url;

                            updates.Add(new SubscriptionUpdate
                            {
                                Subscription = subscription,
                                Name = subscription.Name,
                                LastUpdated = date,
                                Message = title,
                                Url = link
                            });
                            break;
                    }
                }
            }

            return updates.ToArray();
        }
    }
}
