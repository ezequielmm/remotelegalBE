using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class AnnotationEventRepository : BaseRepository<AnnotationEvent>, IAnnotationEventRepository
    {
        public AnnotationEventRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }

        public async Task<List<AnnotationEvent>> GetAnnotationsByDocument(Guid documentId, Guid? annotationId)
        {
            IQueryable<AnnotationEvent> query = _dbContext.Set<AnnotationEvent>().AsNoTracking();
            IQueryable<AnnotationEvent> result;

            string[] include = new[] { nameof(AnnotationEvent.Author) };

            foreach (var property in include)
            {
                query = query.Include(property);
            }

            query = query.Where(x => x.DocumentId == documentId);

            if (annotationId != null)
            {
                var lastIncludedAnnotation = await GetById(annotationId.Value);
                query = query.Where(x => x.CreationDate > lastIncludedAnnotation.CreationDate);
            }

            // Order by ascend
            result = query.OrderBy(x => x.CreationDate);

            var annotationList = await result.ToListAsync().ConfigureAwait(false);

            return annotationList;
        }
    }
}
