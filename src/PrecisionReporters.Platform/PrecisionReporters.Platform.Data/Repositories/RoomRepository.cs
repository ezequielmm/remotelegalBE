using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class RoomRepository : BaseRepository<Room>, IRoomRepository
    {
        public RoomRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
