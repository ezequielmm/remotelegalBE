using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionService
    {
        Task<string> RecognizeAsync(byte[] audioChunk);
    }
}
