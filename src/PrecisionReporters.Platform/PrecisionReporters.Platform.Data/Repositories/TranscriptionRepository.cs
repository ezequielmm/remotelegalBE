using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class TranscriptionRepository : BaseRepository<Transcription>, ITranscriptionRepository
    {
        public TranscriptionRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
