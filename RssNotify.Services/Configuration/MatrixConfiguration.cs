using System;

namespace RssNotify.Services.Configuration
{
    public class MatrixConfiguration
    {
        /// <summary>
        /// Matrix access token. See readme on how to find it.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Matrix room id. See readme on how to find it.
        /// </summary>
        public string RoomId { get; set; }

        /// <summary>
        /// Azure storage table name used to track already delivered notifications.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Need init date as otherwise the notification system goes ballistic on first launch
        /// </summary>
        public DateTimeOffset IgnoreSubscriptionsOlderThan { get; set; }

        /// <summary>
        /// String parsed by <see cref="TimeZoneInfo.FindSystemTimeZoneById" /> to convert UTC to your local time (e.g. "Pacific Time (US & Canada)").
        /// Call <see cref="TimeZoneInfo.GetSystemTimeZones"/> to find your timezone. Note that timezones displayed may differ between local and azure server.
        /// See also https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.getsystemtimezones?view=netcore-3.1
        /// </summary>
        public string TimeZone { get; set; }
    }
}
