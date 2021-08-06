using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class SystemSettingsRepository : BaseRepository<SystemSettings>, ISystemSettingsRepository
    {
        public SystemSettingsRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
