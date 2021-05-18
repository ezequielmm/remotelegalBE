using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly IParticipantRepository _participantRepository;
        private readonly IDepositionRepository _depositionRepository;
        private readonly ISignalRNotificationManager _signalRNotificationManager;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IDepositionEmailService _depositionEmailService;

        public ParticipantService(IParticipantRepository participantRepository,
            ISignalRNotificationManager signalRNotificationManager,
            IUserService userService,
            IDepositionRepository depositionRepository,
            IPermissionService permissionService,
            IDepositionEmailService depositionEmailService)
        {
            _participantRepository = participantRepository;
            _signalRNotificationManager = signalRNotificationManager;
            _userService = userService;
            _depositionRepository = depositionRepository;
            _permissionService = permissionService;
            _depositionEmailService = depositionEmailService;
        }

        public async Task<Result<ParticipantStatusDto>> UpdateParticipantStatus(ParticipantStatusDto participantStatusDto, Guid depositionId)
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                var participant = await _participantRepository.GetFirstOrDefaultByFilter(x => x.UserId == user.Id && x.DepositionId == depositionId);
                if (participant == null)
                {
                    if (!user.IsAdmin)
                    {
                        return Result.Fail(new ResourceNotFoundError("There are no participant available with such userId and depositionId combination."));
                    }

                    var newParticipantAdmin = new Participant
                    {
                        CreationDate = DateTime.UtcNow,
                        DepositionId = depositionId,
                        Email = user.EmailAddress,
                        IsMuted = participantStatusDto.IsMuted,
                        Name = $"{user.FirstName} {user.LastName}",
                        Phone = user.PhoneNumber,
                        Role = ParticipantType.Admin,
                        UserId = user.Id
                    };

                    var result = await _participantRepository.Create(newParticipantAdmin);
                    if (result == null)
                        return Result.Fail(new UnexpectedError("There was an error creating a new Participant."));
                }
                else
                {
                    participant.IsMuted = participantStatusDto.IsMuted;
                    var updatedParticipant = await _participantRepository.Update(participant);
                    if (updatedParticipant == null)
                        return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {participant.Id}"));
                }

                participantStatusDto.Email = user.EmailAddress;
                var notificationDto = new NotificationDto
                {
                    Action = NotificationAction.Update,
                    EntityType = NotificationEntity.ParticipantStatus,
                    Content = participantStatusDto
                };

                await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, notificationDto);

                return Result.Ok(participantStatusDto);
            }
            catch (Exception ex)
            {
                return Result.Fail(new UnexpectedError(ex.Message));
            }
        }

        public async Task<Result<List<Participant>>> GetWaitParticipants(Guid depositionId)
        {
            var includes = new[] { nameof(Participant.User) };
            return Result.Ok(await _participantRepository.GetByFilter(x => x.DepositionId == depositionId && x.HasJoined == false && x.IsAdmitted == null, includes));
        }

        public async Task<Result> RemoveParticipantFromDeposition(Guid id, Guid participantId)
        {
            var deposition = await _depositionRepository.GetById(id,
                new[] { nameof(Deposition.Case), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID {id}"));

            var participant = deposition.Participants.FirstOrDefault(x => x.Id == participantId);
            if (participant == null)
                return Result.Fail(new ResourceNotFoundError($"Participant not found with ID {participantId}"));

            await _permissionService.RemoveParticipantPermissions(id, participant);
            await _participantRepository.Remove(participant);

            if(deposition.Status == DepositionStatus.Confirmed)
                await _depositionEmailService.SendCancelDepositionEmailNotification(deposition, participant);

            return Result.Ok();
        }

        public async Task<Result<Participant>> EditParticipantDetails(Guid depositionId, Participant participant)
        {
            var user = await _userService.GetCurrentUserAsync();
            var deposition = await _depositionRepository.GetById(depositionId, include: new[] {nameof(Deposition.Participants), nameof(Deposition.Case)});
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID: {depositionId}"));

            var hasCourtReporter = deposition.Participants.Any(p => p.Id != participant.Id && Equals(p.Role, ParticipantType.CourtReporter));
            if (hasCourtReporter && Equals(participant.Role, ParticipantType.CourtReporter))
                return Result.Fail(new ResourceConflictError("Only one participant with Court reporter role is available."));

            var currentParticipant = deposition.Participants.FirstOrDefault( p => p.Id == participant.Id);
            if (currentParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There are no participant available with ID: {participant.Id}."));
            var currentParticipantEmail = currentParticipant.Email;

            currentParticipant.Email = participant.Email;
            currentParticipant.Name = participant.Name;
            currentParticipant.Role = participant.Role;
            currentParticipant.Phone = participant.Phone;

            var updatedParticipant = await _participantRepository.Update(currentParticipant);
            if (updatedParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {currentParticipant.Id}"));

            if (deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(updatedParticipant.Email) && !string.Equals(updatedParticipant.Email, currentParticipantEmail))
                await _depositionEmailService.SendJoinDepositionEmailNotification(deposition, updatedParticipant);

            return Result.Ok(updatedParticipant);
        }
    }
}