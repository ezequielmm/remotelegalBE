using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class BreakRoomRepository : BaseRepository<BreakRoom>, IBreakRoomRepository
    {
        public BreakRoomRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
