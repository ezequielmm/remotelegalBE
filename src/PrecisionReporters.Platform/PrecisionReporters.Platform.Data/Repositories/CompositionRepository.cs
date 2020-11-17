using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class CompositionRepository : BaseRepository<Composition>, ICompositionRepository 
    {
        public CompositionRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
