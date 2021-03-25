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
        private readonly ISignalRNotificationManager _signalRNotificationManager;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IDepositionService _depositionService;
        private readonly ICompositionService _compositionService;

        public TranscriptionService(
            ITranscriptionRepository transcriptionRepository,
            IUserRepository userRepository,
            IDepositionDocumentRepository depositionDocumentRepository,
            IDepositionService depositionService,
            ISignalRNotificationManager signalRNotificationManager,
            ICompositionService compositionService,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper)
        {
            _transcriptionRepository = transcriptionRepository;
            _userRepository = userRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
            _depositionService = depositionService;
            _signalRNotificationManager = signalRNotificationManager;
            _compositionService = compositionService;
            _transcriptionMapper = transcriptionMapper;
        }

        public async Task<Result<Transcription>> StoreTranscription(Transcription transcription, string depositionId, string userEmail)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail);

            transcription.DepositionId = new Guid(depositionId);
            transcription.UserId = user.Id;

            var newTranscription = await _transcriptionRepository.Create(transcription);
            var transcriptionDto = _transcriptionMapper.ToDto(newTranscription);
            var notificationtDto = new NotificationDto
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.Transcript,
                Content = transcriptionDto
            };           

            await _signalRNotificationManager.SendNotificationToDepositionMembers(transcriptionDto.DepositionId, notificationtDto);

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
                .Where(x => x.EventType == EventType.OnTheRecord)
                .FirstOrDefault();

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