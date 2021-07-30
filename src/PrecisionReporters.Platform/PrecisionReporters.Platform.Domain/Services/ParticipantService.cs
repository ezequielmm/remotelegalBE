using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
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
        private readonly IDeviceInfoRepository _deviceInfoRepository;
        private readonly ISignalRDepositionManager _signalRNotificationManager;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IDepositionEmailService _depositionEmailService;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        public ParticipantService(IParticipantRepository participantRepository,
            ISignalRDepositionManager signalRNotificationManager,
            IUserService userService,
            IDepositionRepository depositionRepository,
            IDeviceInfoRepository deviceInfoRepository,
            IPermissionService permissionService,
            IDepositionEmailService depositionEmailService,
            IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper)
        {
            _participantRepository = participantRepository;
            _signalRNotificationManager = signalRNotificationManager;
            _userService = userService;
            _depositionRepository = depositionRepository;
            _deviceInfoRepository = deviceInfoRepository;
            _permissionService = permissionService;
            _depositionEmailService = depositionEmailService;
            _participantMapper = participantMapper;
        }

        public async Task<Result<ParticipantStatusDto>> UpdateParticipantStatus(ParticipantStatusDto participantStatusDto, Guid depositionId)
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                var participantToUpdateStatus = await GetParticipantToUpdate(depositionId, user);
                if (participantToUpdateStatus.IsFailed)
                    return participantToUpdateStatus.ToResult();

                participantToUpdateStatus.Value.IsMuted = participantStatusDto.IsMuted;
                var updatedParticipant = await _participantRepository.Update(participantToUpdateStatus.Value);
                if (updatedParticipant == null)
                    return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {participantToUpdateStatus.Value.Id}"));

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

        private async Task<Result<Participant>> GetParticipantToUpdate(Guid depositionId, User user)
        {
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
                    Name = $"{user.FirstName} {user.LastName}",
                    Phone = user.PhoneNumber,
                    Role = ParticipantType.Admin,
                    UserId = user.Id
                };

                var createParticipantResult = await _participantRepository.Create(newParticipantAdmin);
                if (createParticipantResult == null)
                    return Result.Fail(new UnexpectedError("There was an error creating a new Participant."));

                return Result.Ok(createParticipantResult);
            }

            return Result.Ok(participant);
        } 

        public async Task<Result<ParticipantStatusDto>> NotifyParticipantPresence(ParticipantStatusDto participantStatusDto, Guid depositionId)
        {
            var user = await _userService.GetCurrentUserAsync();
            var participantResult = await GetParticipantToUpdate(depositionId, user);

            var shouldSendAdminsNotifications = !participantResult.Value.IsAdmitted.HasValue ||
                (participantResult.Value.IsAdmitted.HasValue && !participantResult.Value.IsAdmitted.Value);

            if (shouldSendAdminsNotifications)
            {
                participantResult.Value.HasJoined = false;
                var notificationtDto = new NotificationDto
                {
                    Action = NotificationAction.Create,
                    EntityType = NotificationEntity.JoinRequest,
                    Content = _participantMapper.ToDto(participantResult.Value)
                };
                await _signalRNotificationManager.SendNotificationToDepositionAdmins(depositionId, notificationtDto);
            }

            participantResult.Value.IsMuted = participantStatusDto.IsMuted;
            await _participantRepository.Update(participantResult.Value);
            
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

        public async Task<Result<List<Participant>>> GetWaitParticipants(Guid depositionId)
        {
            var includes = new[] { nameof(Participant.User) };
            return Result.Ok(await _participantRepository.GetByFilter(x => x.DepositionId == depositionId && !x.HasJoined && x.IsAdmitted == null, includes));
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

            if(deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(participant.Email))
                await _depositionEmailService.SendCancelDepositionEmailNotification(deposition, participant);

            return Result.Ok();
        }

        public async Task<Result<Participant>> EditParticipantDetails(Guid depositionId, Participant participant)
        {
            var deposition = await _depositionRepository.GetById(depositionId, include: new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case)});
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

            if (currentParticipant?.User?.IsGuest == null)
            {
                currentParticipant.Name = participant.Name;
                currentParticipant.Phone = participant.Phone;
            }

            var updatedParticipant = await _participantRepository.Update(currentParticipant);
            if (updatedParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {currentParticipant.Id}"));

            if (deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(updatedParticipant.Email) && !string.Equals(updatedParticipant.Email, currentParticipantEmail))
                await _depositionEmailService.SendJoinDepositionEmailNotification(deposition, updatedParticipant);

            return Result.Ok(updatedParticipant);
        }

        public async Task<Result> SetUserDeviceInfo(Guid depositionId, DeviceInfo userDeviceInfo)
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            var participant = await GetParticipantToUpdate(depositionId, currentUser);
            if (participant.IsFailed)
                return participant.ToResult();

            var deviceInfo = await _deviceInfoRepository.Create(userDeviceInfo);

            participant.Value.DeviceInfoId = deviceInfo.Id;

            var updatedParticipant = await _participantRepository.Update(participant.Value);
            if (updatedParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {participant.Value.Id}"));

            return Result.Ok();
        }
    }
}