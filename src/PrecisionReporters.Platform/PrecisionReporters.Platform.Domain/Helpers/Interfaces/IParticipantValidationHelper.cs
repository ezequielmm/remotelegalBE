using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Helpers.Interfaces
{
    public interface IParticipantValidationHelper
    {
        public Task<Result<Deposition>> GetValidDepositionForEditParticipantRoleAsync(Guid depositionId);
        public Result<Participant> GetValidTargetParticipantForEditRole(Deposition deposition, Participant participant);
    }
}