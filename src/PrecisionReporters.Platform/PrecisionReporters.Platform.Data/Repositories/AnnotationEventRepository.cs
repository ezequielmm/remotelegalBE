using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class AnnotationEventRepository : BaseRepository<AnnotationEvent>, IAnnotationEventRepository
    {
        public AnnotationEventRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }
    }
}
