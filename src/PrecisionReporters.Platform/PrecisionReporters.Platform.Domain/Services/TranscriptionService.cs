using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly ITranscriptionRepository _transcriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IDepositionService _depositionService;
        private readonly ICompositionService _compositionService;

        public TranscriptionService(
            ITranscriptionRepository transcriptionRepository,
            IUserRepository userRepository,
            IDepositionDocumentRepository depositionDocumentRepository,
            IParticipantRepository participantRepository,
            IDepositionService depositionService,
            ICompositionService compositionService)
        {
            _transcriptionRepository = transcriptionRepository;
            _userRepository = userRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
            _participantRepository = participantRepository;
            _depositionService = depositionService;
            _compositionService = compositionService;
        }

        public async Task<Result<Transcription>> StoreTranscription(Transcription transcription, string depositionId, string userEmail)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail);

            if (user == null)
                return Result.Fail("User with such email address was not found.");

            var newTranscription = await _transcriptionRepository.Create(transcription);
            if (newTranscription == null)
                return Result.Fail("Fail to create new transcription.");           

            return Result.Ok(transcription);
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

        public async Task<Result<List<DepositionDocument>>> GetTranscriptionsFiles(Guid depostionId, string identity)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == identity);
            var currentParticipant = await _participantRepository.GetFirstOrDefaultByFilter(x => x.UserId == user.Id);
            var includes = new[] { $"{ nameof(DepositionDocument.Document) }.{ nameof(Document.AddedBy) }" };
            Expression<Func< DepositionDocument, bool>> filter;

            if (currentParticipant.Role == ParticipantType.CourtReporter || user.IsAdmin)
            {
                 filter = x => x.DepositionId == depostionId && (x.Document.DocumentType == DocumentType.DraftTranscription || x.Document.DocumentType == DocumentType.DraftTranscriptionWord || x.Document.DocumentType == DocumentType.Transcription);
            }
            else 
            {
                filter = x => x.DepositionId == depostionId && (x.Document.DocumentType == DocumentType.DraftTranscription || x.Document.DocumentType == DocumentType.Transcription);
            }
            var documentsResult = await _depositionDocumentRepository.GetByFilter(x => x.CreationDate,
                SortDirection.Ascend,
                filter,
                includes);

            return Result.Ok(documentsResult);
        }

        public async Task<Result<List<TranscriptionTimeDto>>> GetTranscriptionsWithTimeOffset(Guid depositionId)
        {
            var include = new[] { nameof(Deposition.Room), nameof(Deposition.Events) };
            var depositionResult = await _depositionService.GetByIdWithIncludes(depositionId, include);

            if (depositionResult.IsFailed)
                return depositionResult.ToResult<List<TranscriptionTimeDto>>();

            var transcriptionsResult = await GetTranscriptionsByDepositionId(depositionId);
            if (transcriptionsResult.IsFailed)
                return transcriptionsResult.ToResult<List<TranscriptionTimeDto>>();

            var deposition = depositionResult.Value;
            var startedAt =  VideoStartDate(deposition.Events);
            var compositionIntervals = _compositionService.GetDepositionRecordingIntervals(deposition.Events, startedAt);

            var resultList = transcriptionsResult.Value
                .OrderBy(x => x.CreationDate)
                .Select(x =>
                    new TranscriptionTimeDto
                    {
                        Text = x.Text,
                        Id = x.Id.ToString(),
                        Confidence = x.Confidence,
                        TranscriptDateTime = new DateTimeOffset(x.CreationDate, TimeSpan.Zero),
                        TranscriptionVideoTime = CalculateSpeechTime(
                            CalculateSeconds(startedAt, GetDateTimestamp(x.TranscriptDateTime)),
                            compositionIntervals),
                        UserName = x.User.GetFullName(),
                        Duration = x.Duration
                    }

                ).ToList();

            return Result.Ok(resultList);
        }

        private long VideoStartDate(List<DepositionEvent> events)
        {
            var result = events?
                .OrderBy(x => x.CreationDate)
                .FirstOrDefault(x => x.EventType == EventType.OnTheRecord);

            if (result == null)
                return 0;

            return GetDateTimestamp(result.CreationDate);
        }

        private int CalculateSeconds(long startTime, long splitTime)
        {
            return (int)(splitTime - startTime);
        }

        private long GetDateTimestamp(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeSeconds();
        }

        private int CalculateSpeechTime(int offset, List<CompositionInterval> intervals)
        {
            var t = 0;
            for (var i = 0; i < intervals.Count; i++) 
            {
                if (intervals[i].Stop < offset && i != (intervals.Count -1))
                {
                    t += (intervals[i + 1].Start - intervals[i].Stop);
                }
            }
            return offset - t;
        }
    }
}