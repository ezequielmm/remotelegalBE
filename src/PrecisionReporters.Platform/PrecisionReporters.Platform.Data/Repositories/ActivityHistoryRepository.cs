using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class ActivityHistoryRepository : BaseRepository<ActivityHistory>, IActivityHistoryRepository
    {
        public ActivityHistoryRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
