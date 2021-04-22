using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Transcripts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DraftTranscriptGeneratorService : IDraftTranscriptGeneratorService
    {
        private readonly ITranscriptionService _transcriptionService;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IEnumerable<IRoughTranscriptGenerator> _transcriptGenerators;
        private readonly ILogger<DraftTranscriptGeneratorService> _logger;

        public DraftTranscriptGeneratorService(ITranscriptionService transcriptionService,
            IDepositionRepository depositionRepository, IEnumerable<IRoughTranscriptGenerator> transcriptGenerators,
            ILogger<DraftTranscriptGeneratorService> logger)
        {
            _transcriptionService = transcriptionService;
            _depositionRepository = depositionRepository;
            _transcriptGenerators = transcriptGenerators;
            _logger = logger;
        }

        public async Task<Result> GenerateDraftTranscription(DraftTranscriptDto draftTranscriptDto)
        {
            var include = new[] { nameof(Deposition.Case), nameof(Deposition.Participants), nameof(Deposition.Requester), nameof(Deposition.Events) };
            var transcriptions = await _transcriptionService.GetTranscriptionsByDepositionId(draftTranscriptDto.DepositionId);
            var deposition = await _depositionRepository.GetFirstOrDefaultByFilter(x => x.Id == draftTranscriptDto.DepositionId, include);
            try
            {
                var tasks = _transcriptGenerators.Select(x => x.GenerateTranscriptTemplate(draftTranscriptDto, deposition, transcriptions.Value));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Result.Fail(new ExceptionalError("Error generating file form stream.", ex));
            }

            return Result.Ok();
        }
    }
}
