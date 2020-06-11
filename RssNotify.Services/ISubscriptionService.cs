using RssNotify.Services.Models;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public interface ISubscriptionService
    {
        /// <summary>
        /// When called will check all subscriptions for updates.
        /// Takes into consideration the previous updates (to prevent double posting).
        /// </summary>
        /// <param name="cancellationToken"></param>
        Task<SubscriptionUpdate[]> GetLatestAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Marks an update so it isn't returned again in the future.
        /// Should only be updated once the user has received the update
        /// </summary>
        Task MarkAsReceivedAsync(SubscriptionUpdate update, CancellationToken cancellationToken);
    }
}
