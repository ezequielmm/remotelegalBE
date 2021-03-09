using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICompositionService
    {
        Task<Composition> UpdateComposition(Composition composition);
        Task<Result<Composition>> GetCompositionByRoom(Guid roomSid);
        Task<Result> StoreCompositionMediaAsync(Composition composition);
        Task<Result<Composition>> UpdateCompositionCallback(Composition composition);
        Task<Result> PostDepoCompositionCallback(PostDepositionEditionDto payload);
        Task<Result> SubscribeEndpoint(string subscribeURL);
        List<CompositionInterval> GetDepositionRecordingIntervals(List<DepositionEvent> events, long startTime);
    }
}
