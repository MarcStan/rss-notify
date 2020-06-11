using System.Threading;
using System.Threading.Tasks;

namespace RssNotify.Services
{
    public interface IMatrixNotificationService
    {
        Task SendAsync(string message, CancellationToken cancellationToken);
    }
}
