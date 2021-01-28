using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly ITranscriptionRepository _transcriptionRepository;
        private readonly IUserRepository _userRepository;


        public TranscriptionService(ITranscriptionRepository transcriptionRepository, IUserRepository userRepository)
        {
            _transcriptionRepository = transcriptionRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<Transcription>> StoreTranscription(Transcription transcription, string depositionId, string userEmail)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == userEmail);

            transcription.DepositionId = new Guid(depositionId);
            transcription.UserId = user.Id;
            transcription.Text = transcription.Text;

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

    }
}