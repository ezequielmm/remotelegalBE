using System;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;

namespace PrecisionReporters.Platform.Domain.Helpers
{
    public class ParticipantValidationHelper : IParticipantValidationHelper
    {
        private readonly IDepositionRepository _depositionRepository;

        public ParticipantValidationHelper(IDepositionRepository depositionRepository)
        {
            _depositionRepository = depositionRepository;
        }

        public async Task<Result<Deposition>> GetValidDepositionForEditParticipantRoleAsync(Guid depositionId)
        {
            var deposition = await _depositionRepository.GetById(depositionId,
            include: new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case), nameof(Deposition.Events), nameof(Deposition.Room) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID: {depositionId} ."));

            if (deposition.IsOnTheRecord)
                return Result.Fail(new InvalidInputError("IsOnTheRecord A role change cannot be made if Deposition is currently on the record."));

            return Result.Ok(deposition);
        }

        public Result<Participant> GetValidTargetParticipantForEditRole(Deposition deposition, Participant participant)
        {
            var hasCourtReporter = deposition.Participants.Any(p => p.Email != participant.Email && Equals(p.Role, ParticipantType.CourtReporter));
            if (hasCourtReporter && Equals(participant.Role, ParticipantType.CourtReporter))
                return Result.Fail(new InvalidInputError("Only one participant with Court reporter role is allowed."));

            var targetParticipant = deposition.Participants.FirstOrDefault(p => p.Email == participant.Email);
            if (targetParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There are no participant available with email: {participant.Email}."));

            if (targetParticipant.Role == participant.Role)
                return Result.Fail(new InvalidInputError("Participant already have requested role."));

            //TODO: This is a restriction for this increment. Once the depo was on the record, witness cannot be exchanged
            if (targetParticipant.Role == ParticipantType.Witness && (deposition.Events?.Any(e => e.EventType == EventType.OnTheRecord) ?? false))
                return Result.Fail(new InvalidInputError("IsOnTheRecord A Witness participant cannot be exchanged if Deposition has been on the record."));

            if (participant.Role == ParticipantType.Witness && deposition.Participants.Any(p => p.Role == ParticipantType.Witness && !string.IsNullOrWhiteSpace(p.Email)))
                return Result.Fail(new ResourceConflictError("Only one participant with Witness role is allowed."));

            return Result.Ok(targetParticipant);
        }
    }
}