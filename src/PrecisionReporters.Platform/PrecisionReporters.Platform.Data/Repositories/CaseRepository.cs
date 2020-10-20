using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class CaseRepository : BaseRepository<Case>, ICaseRepository
    {
        public CaseRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
