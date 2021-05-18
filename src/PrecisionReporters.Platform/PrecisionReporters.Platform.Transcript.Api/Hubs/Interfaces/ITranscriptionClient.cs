using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs.Interfaces
{
    public interface ITranscriptionClient
    {
        /// <summary>
        /// Receives a <see cref="NotificationDto"/>.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task ReceiveTranscription(NotificationDto notification);
    }
}
