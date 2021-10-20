using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            if (annotationId.HasValue)
            {
                var lastIncludedAnnotation = await _annotationEventRepository.GetById(annotationId.Value);
                if (lastIncludedAnnotation == null)
                    return Result.Fail(new ResourceNotFoundError($"annotation with Id {annotationId} could not be found"));
            }

            var annotations = await _annotationEventRepository.GetAnnotationsByDocument(documentId, annotationId);

            return Result.Ok(annotations);
        }

        public async Task<Result> RemoveUserDocumentAnnotations(Guid documentId)
        {
            var annotationEventList = await _annotationEventRepository.GetByFilter(x => x.DocumentId == documentId);
            await _annotationEventRepository.RemoveRange(annotationEventList);
            return Result.Ok();
        }
    }
}
