using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Helpers.Interfaces
{
    public interface IParticipantValidationHelper
    {
        public Task<Result<Deposition>> GetValidDepositionForEditParticipantAsync(Guid depositionId);
        public Result ValidateTargetParticipantForEditRole(Deposition deposition, Participant participant, Participant targetParticipant);
    }
}