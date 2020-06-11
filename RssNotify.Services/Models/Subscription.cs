namespace RssNotify.Services.Models
{
    /// <summary>
    /// A subscription describes a particular resource that is polled periodically for updates.
    /// </summary>
    public class Subscription
    {
        public string Type { get; set; }

        /// <summary>
        /// Human readable name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Website to poll for changes.
        /// </summary>
        public string Url { get; set; }
    }
}
