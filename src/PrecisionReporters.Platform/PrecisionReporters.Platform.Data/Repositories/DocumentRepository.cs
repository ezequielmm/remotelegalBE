using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DocumentRepository: BaseRepository<DepositionDocument>, IDocumentRepository
    {
        public DocumentRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
