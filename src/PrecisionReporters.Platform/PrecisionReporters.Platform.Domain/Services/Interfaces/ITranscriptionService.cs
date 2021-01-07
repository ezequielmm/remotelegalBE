using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionService
    {
        Task<TranscriptionDto> RecognizeAsync(byte[] audioChunk, string userEmail, string depositionId);
    }
}
