using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DepositionRepository : BaseRepository<Deposition>, IDepositionRepository
    {
        public DepositionRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
