using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IAnnotationEventRepository : IRepository<AnnotationEvent>
    {
        Task<List<AnnotationEvent>> GetAnnotationsByDocument(Guid documentId, Guid? annotationId);
    }
}
