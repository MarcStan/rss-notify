using Microsoft.Azure.Cosmos.Table;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RssNotify.Services.Models
{
    public class ReceivedSubscriptionModel : TableEntity
    {
        public ReceivedSubscriptionModel()
        {
        }

        public ReceivedSubscriptionModel(SubscriptionUpdate update)
        {
            Name = update.Name;
            Source = update.Subscription.Url;
            Url = update.Url;
            Type = update.Subscription.Type;
            ReceivedAt = update.LastUpdated;

            PartitionKey = GetPartitionKey();
            RowKey = GetRowKey();
        }

        private string GetRowKey()
        {
            var sb = new StringBuilder();
            using (var algorithm = SHA256.Create())
                foreach (var b in algorithm.ComputeHash(Encoding.UTF8.GetBytes(Url.ToLowerInvariant())))
                    sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private string GetPartitionKey()
        {
            var name = Name.Replace(" ", "").ToLowerInvariant();
            return $"{Type.ToLowerInvariant()}_{name.Substring(0, Math.Min(4, name.Length))}";
        }

        public string Type { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Source { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }
    }
}
