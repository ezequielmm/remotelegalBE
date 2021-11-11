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

        public async Task<Result<Deposition>> GetValidDepositionForEditParticipantAsync(Guid depositionId)
        {
            var deposition = await _depositionRepository.GetById(depositionId,
            include: new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case), nameof(Deposition.Events), nameof(Deposition.Room) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID: {depositionId} ."));

            if (deposition.IsOnTheRecord)
                return Result.Fail(new InvalidInputError("IsOnTheRecord A participant edit cannot be made if Deposition is currently on the record."));

            return Result.Ok(deposition);
        }

        public Result ValidateTargetParticipantForEditRole(Deposition deposition, Participant participant, Participant targetParticipant)
        {
            var hasCourtReporter = deposition.Participants.Any(p => p.Email != participant.Email && p.Role == ParticipantType.CourtReporter);
            if (hasCourtReporter && participant.Role == ParticipantType.CourtReporter)
                return Result.Fail(new InvalidInputError("Only one participant with Court reporter role is allowed."));

            if (targetParticipant.Role == participant.Role)
                return Result.Fail(new InvalidInputError("Participant already have requested role."));

            //TODO: This is a restriction for this increment. Once the depo was on the record, witness cannot be exchanged
            var hasBeenOnTheRecord = deposition.Events?.Any(e => e.EventType == EventType.OnTheRecord) ?? false;
            if ((participant.Role == ParticipantType.Witness || targetParticipant.Role == ParticipantType.Witness) && hasBeenOnTheRecord)
                return Result.Fail(new InvalidInputError("HasBeenOnTheRecord A Witness participant cannot be exchanged if Deposition has been on the record."));

            return Result.Ok(targetParticipant);
        }
    }
}