using System;

namespace RssNotify.Services.Configuration
{
    public class MatrixConfiguration
    {
        public string AccessToken { get; set; }

        public string RoomId { get; set; }

        public string TableName { get; set; }

        /// <summary>
        /// Need init date as otherwise the notification system goes ballistic on first launch
        /// </summary>
        public DateTimeOffset IgnoreSubscriptionsOlderThan { get; set; }
    }
}
