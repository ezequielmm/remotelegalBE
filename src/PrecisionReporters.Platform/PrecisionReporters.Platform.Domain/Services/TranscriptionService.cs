using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Shared.Errors;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly ITranscriptionRepository _transcriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IDepositionRepository _depositionRepository;
        private readonly ICompositionHelper _compositionHelper;
        private readonly ILogger<TranscriptionService> _logger;

        public TranscriptionService(
            ITranscriptionRepository transcriptionRepository,
            IUserRepository userRepository,
            IDepositionDocumentRepository depositionDocumentRepository,
            IParticipantRepository participantRepository,
            IDepositionRepository depositionRepository,
            ICompositionHelper compositionHelper,
            ILogger<TranscriptionService> logger)
        {
            _transcriptionRepository = transcriptionRepository;
            _userRepository = userRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
            _participantRepository = participantRepository;
            _depositionRepository = depositionRepository;
            _compositionHelper = compositionHelper;
            _logger = logger;
        }

        public async Task<Result<Transcription>> StoreTranscription(Transcription transcription, string depositionId, string userEmail)
        {
            try
            {
                var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail);

                if (user == null)
                {
                    _logger.LogError("User with email address {0} was not found.", userEmail);
                    return Result.Fail("User with such email address was not found.");
                }

                _logger.LogInformation("Start Create Transcription");
                var newTranscription = await _transcriptionRepository.Create(transcription);
                _logger.LogInformation("End Create Transcription");

                if (newTranscription == null)
                {
                    _logger.LogError("Fail to create new transcription with Id:{0} Deposition Id:{1} User Id:{2}.", transcription.Id, depositionId, transcription.UserId);
                    return Result.Fail("Fail to create new transcription.");
                }

                return Result.Ok(transcription);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An was found while storing transcriptons. {ex}");
                throw;
            }
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

        public async Task<Result<List<DepositionDocument>>> GetTranscriptionsFiles(Guid depositionId, string identity)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == identity);
            var currentParticipant = await _participantRepository.GetFirstOrDefaultByFilter(x => x.DepositionId == depositionId && x.UserId == user.Id);
            var includes = new[] { $"{ nameof(DepositionDocument.Document) }.{ nameof(Document.AddedBy) }" };
            Expression<Func<DepositionDocument, bool>> filter;

            if (user.IsAdmin || currentParticipant.Role == ParticipantType.CourtReporter)
            {
                filter = x => x.DepositionId == depositionId && (x.Document.DocumentType == DocumentType.DraftTranscription || x.Document.DocumentType == DocumentType.DraftTranscriptionWord || x.Document.DocumentType == DocumentType.Transcription);
            }
            else
            {
                filter = x => x.DepositionId == depositionId && (x.Document.DocumentType == DocumentType.DraftTranscription || x.Document.DocumentType == DocumentType.Transcription);
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
            var depositionResult = await _depositionRepository.GetById(depositionId, include);

            if (depositionResult == null)
            {
                _logger.LogError("Unable to find deposition with Id: {0}", depositionId);
                return Result.Fail($"Unable to find deposition with Id {depositionId}");
            }

            var transcriptionsResult = await GetTranscriptionsByDepositionId(depositionId);

            var deposition = depositionResult;
            var startedAt = VideoStartDate(deposition.Events);
            var startedDate = DateTimeOffset.FromUnixTimeMilliseconds(startedAt);
            var compositionIntervals = _compositionHelper.GetDepositionRecordingIntervals(deposition.Events, startedDate.UtcDateTime);

            var resultList = transcriptionsResult.Value
                .OrderBy(x => x.CreationDate)
                .Select(x =>
                    new TranscriptionTimeDto
                    {
                        Text = x.Text,
                        Id = x.Id.ToString(),
                        Confidence = x.Confidence,
                        TranscriptDateTime = new DateTimeOffset(x.TranscriptDateTime, TimeSpan.Zero),
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
            return new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        private int CalculateSpeechTime(int offset, List<CompositionInterval> intervals)
        {
            var t = 0;
            for (var i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Stop < offset && i != (intervals.Count - 1))
                {
                    t += (intervals[i + 1].Start - intervals[i].Stop);
                }
            }
            return offset - t;
        }
    }
}