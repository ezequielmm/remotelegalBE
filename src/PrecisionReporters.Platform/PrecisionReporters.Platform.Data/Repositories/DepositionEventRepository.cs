using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DepositionEventRepository : BaseRepository<DepositionEvent>, IDepositionEventRepository
    {
        public DepositionEventRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
