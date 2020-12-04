using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class ParticipantRepository : BaseRepository<Participant>, IParticipantRepository
    {
        public ParticipantRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
