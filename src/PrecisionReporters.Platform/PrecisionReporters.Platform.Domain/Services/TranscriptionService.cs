using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly ITranscriptionRepository _transcriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;


        public TranscriptionService(ITranscriptionRepository transcriptionRepository, IUserRepository userRepository, IDepositionDocumentRepository depositionDocumentRepository)
        {
            _transcriptionRepository = transcriptionRepository;
            _userRepository = userRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
        }

        public async Task<Result<Transcription>> StoreTranscription(Transcription transcription, string depositionId, string userEmail)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail);

            transcription.DepositionId = new Guid(depositionId);
            transcription.UserId = user.Id;

            var newTranscription = await _transcriptionRepository.Create(transcription);
            return Result.Ok(newTranscription);
        }

        public async Task<Result<List<Transcription>>> GetTranscriptionsByDepositionId(Guid depositionId)
        {
            var include = new[] { nameof(Transcription.User) };
            var result = await _transcriptionRepository.GetByFilter(
                x => x.TranscriptDateTime,
                SortDirection.Ascend,
                x => x.DepositionId == depositionId,
                include);

            return Result.Ok(result);
        }

        public async Task<Result<List<DepositionDocument>>> GetTranscriptionsFiles(Guid depostionId)
        {
            var includes = new[] { $"{ nameof(DepositionDocument.Document) }.{ nameof(Document.AddedBy) }" };
            Expression<Func<DepositionDocument, bool>> filter = x => x.DepositionId == depostionId && (x.Document.DocumentType == DocumentType.DraftTranscription || x.Document.DocumentType == DocumentType.Transcription);

            var documentsResult = await _depositionDocumentRepository.GetByFilter(x => x.CreationDate,
                SortDirection.Ascend,
                filter,
                includes);

            return Result.Ok(documentsResult);
        }

    }
}