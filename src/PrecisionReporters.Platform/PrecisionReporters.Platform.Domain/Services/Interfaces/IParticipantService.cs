using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IParticipantService
    {
        Task<Result<ParticipantStatusDto>> UpdateParticipantStatus(ParticipantStatusDto participantStatusDto, Guid depositionId);
    }
}
