using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Hubs.Interfaces
{
    public interface IDepositionClient
    {
        /// <summary>
        /// Receives a <see cref="NotificationDto"/>.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task ReceiveNotification(NotificationDto notification);
    }
}
