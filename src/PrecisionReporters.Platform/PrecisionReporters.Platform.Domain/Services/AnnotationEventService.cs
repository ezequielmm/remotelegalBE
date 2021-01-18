using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class AnnotationEventService : IAnnotationEventService
    {
        private readonly IAnnotationEventRepository _annotationEventRepository;
        private readonly IDepositionService _depositionService;

        public AnnotationEventService(IAnnotationEventRepository annotationEventRepository, IDepositionService depositionService)
        {
            _annotationEventRepository = annotationEventRepository;
            _depositionService = depositionService;
        }

        public async Task<Result<List<AnnotationEvent>>> GetDocumentAnnotations(Guid depositionId, Guid? annotationId)
        {
            // TODO include sharingDocument
            var depositionResult = await _depositionService.GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<List<AnnotationEvent>>();

            if (!depositionResult.Value.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceNotFoundError($"There is no shared document for deposition {depositionId}"));

            var documentId = depositionResult.Value.SharingDocumentId.Value;
            var include = new[] { nameof(AnnotationEvent.Author) };
            Expression<Func<AnnotationEvent, bool>> filter = x => x.DocumentId == documentId;

            if (annotationId.HasValue)
            {
                var lastIncludedAnnotation = await _annotationEventRepository.GetById(annotationId.Value);
                if (lastIncludedAnnotation == null)
                    return Result.Fail(new ResourceNotFoundError($"annoitation with Id {annotationId} could not be found"));

                filter = x => x.DocumentId == documentId && x.CreationDate > lastIncludedAnnotation.CreationDate;
            }

            var annotations = await _annotationEventRepository.GetByFilter(x => x.CreationDate, SortDirection.Ascend, filter, include);

            return Result.Ok(annotations);
        }
    }
}
