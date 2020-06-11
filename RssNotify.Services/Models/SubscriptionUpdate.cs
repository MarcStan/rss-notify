using System;

namespace RssNotify.Services.Models
{
    public class SubscriptionUpdate
    {
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Human readable subscription name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url that was polled for updates.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The latest update
        /// </summary>
        public string Message { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }
}
