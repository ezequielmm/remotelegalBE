using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DepositionDocumentRepository: BaseRepository<DepositionDocument>, IDepositionDocumentRepository
    {
        public DepositionDocumentRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
