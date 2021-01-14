using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAnnotationEventService
    {
        Task<Result<List<AnnotationEvent>>> GetDocumentAnnotations(Guid documentId, Guid? annotationId);
    }
}
