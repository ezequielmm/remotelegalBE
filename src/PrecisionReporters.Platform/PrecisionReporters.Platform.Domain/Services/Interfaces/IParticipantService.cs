using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IParticipantService
    {
        Task<Result<ParticipantStatusDto>> UpdateParticipantStatus(ParticipantStatusDto participantStatusDto, Guid depositionId);
        Task<Result<List<Participant>>> GetWaitParticipants(Guid depositionId);
    }
}
