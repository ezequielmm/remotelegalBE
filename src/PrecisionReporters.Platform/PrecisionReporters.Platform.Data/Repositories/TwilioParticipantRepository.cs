using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class TwilioParticipantRepository : BaseRepository<TwilioParticipant>, ITwilioParticipantRepository
    {
        public TwilioParticipantRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
