using System;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CompositionService : ICompositionService
    {
        private readonly ICompositionRepository _compositionRepository;
        private readonly ITwilioService _twilioService;

        public CompositionService(ICompositionRepository compositionRepository,
            ITwilioService twilioService)
        {
            _compositionRepository = compositionRepository;
            _twilioService = twilioService;
        }

        public async Task<Result> StoreCompositionMediaAsync(Composition composition)
        {
            composition.EndDate = DateTime.UtcNow;
            var couldDownloadComposition = await _twilioService.GetCompositionMediaAsync(composition);
            if (!couldDownloadComposition)
                return Result.Fail(new ExceptionalError("Could now download composition media.", null));

            composition.Status = CompositionStatus.Uploading;
            var updatedComposition = await UpdateComposition(composition);

            await _twilioService.UploadCompositionMediaAsync(updatedComposition);
            return Result.Ok();
        }

        public async Task<Result<Composition>> GetCompositionByRoom(Guid roomId)
        {
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.RoomId == roomId);
            if (composition == null)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(composition);
        }

        public async Task<Composition> UpdateComposition(Composition composition)
        {
            composition.LastUpdated = DateTime.UtcNow;
            return await _compositionRepository.Update(composition);
        }

        public async Task<Result<Composition>> UpdateCompositionCallback(Composition compositionUpdated)
        {
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.SId == compositionUpdated.SId);
            if (composition == null)
                return Result.Fail(new ResourceNotFoundError());

            composition.Status = compositionUpdated.Status;

            if (composition.Status == CompositionStatus.Available)
            {
                composition.MediaUri = compositionUpdated.MediaUri;
                var storeCompositionResult = await StoreCompositionMediaAsync(composition);
                composition.Status = storeCompositionResult.IsSuccess
                    ? CompositionStatus.Stored
                    : CompositionStatus.UploadFailed;
            }

            var updatedComposition = await UpdateComposition(composition);
            return Result.Ok(updatedComposition);
        }
    }
}
