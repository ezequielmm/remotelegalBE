using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DocumentUserDepositionRepository : BaseRepository<DocumentUserDeposition>, IDocumentUserDepositionRepository
    {
        public DocumentUserDepositionRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
