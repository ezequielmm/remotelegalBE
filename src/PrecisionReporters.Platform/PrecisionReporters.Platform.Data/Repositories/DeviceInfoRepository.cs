using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DeviceInfoRepository : BaseRepository<DeviceInfo>, IDeviceInfoRepository
    {
        public DeviceInfoRepository(ApplicationDbContext dbContext) : base(dbContext)
        { }
    }
}
