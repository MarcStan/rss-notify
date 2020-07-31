using RssNotify.Services.Models;
using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public interface ISubscriptionProvider
    {
        Task<Subscription[]> GetSubscriptionsAsync(CancellationToken cancellationToken);
    }
}
