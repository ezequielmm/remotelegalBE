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
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;

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
        private readonly IParticipantValidationHelper _participantValidationHelper;
        private readonly IRoomService _roomService;
        private readonly ILogger<ParticipantService> _logger;
        public ParticipantService(IParticipantRepository participantRepository,
            ISignalRDepositionManager signalRNotificationManager,
            IUserService userService,
            IDepositionRepository depositionRepository,
            IDeviceInfoRepository deviceInfoRepository,
            IPermissionService permissionService,
            IDepositionEmailService depositionEmailService,
            IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper,
            IParticipantValidationHelper participantValidationHelper,
            IRoomService roomService,
            ILogger<ParticipantService> logger)
        {
            _participantRepository = participantRepository;
            _signalRNotificationManager = signalRNotificationManager;
            _userService = userService;
            _depositionRepository = depositionRepository;
            _deviceInfoRepository = deviceInfoRepository;
            _permissionService = permissionService;
            _depositionEmailService = depositionEmailService;
            _participantMapper = participantMapper;
            _participantValidationHelper = participantValidationHelper;
            _roomService = roomService;
            _logger = logger;
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
                    Name = user.FirstName,
                    LastName = user.LastName,
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

            if (participantResult.IsFailed)
            {
                return Result.Fail(participantResult.Errors.LastOrDefault() ?? new UnexpectedError("Unable to find or create participant."));
            }

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
                if (!user.IsAdmin)
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

            if (deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(participant.Email))
                await _depositionEmailService.SendCancelDepositionEmailNotification(deposition, participant);

            return Result.Ok();
        }

        public async Task<Result<Participant>> EditParticipantDetails(Guid depositionId, Participant participant)
        {
            var deposition = await _depositionRepository.GetById(depositionId, include: new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID: {depositionId}"));

            var hasCourtReporter = deposition.Participants.Any(p => p.Id != participant.Id && Equals(p.Role, ParticipantType.CourtReporter));
            if (hasCourtReporter && Equals(participant.Role, ParticipantType.CourtReporter))
                return Result.Fail(new ResourceConflictError("Only one participant with Court reporter role is available."));

            var currentParticipant = deposition.Participants.FirstOrDefault(p => p.Id == participant.Id);
            if (currentParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There are no participant available with ID: {participant.Id}."));
            var currentParticipantEmail = currentParticipant.Email;
            var emailChanged = (currentParticipant.Email != participant.Email);

            currentParticipant = await ManageEditParticipantPermission(emailChanged, depositionId, currentParticipant, participant);
            currentParticipant = HandleEditFullName(participant, currentParticipant, emailChanged);
            
            currentParticipant.Email = participant.Email;
            currentParticipant.Role = participant.Role;
            currentParticipant.Phone = participant.Phone;
            
            if (currentParticipant?.User?.IsGuest == null)
            {
                currentParticipant.Name = participant.Name;
                currentParticipant.LastName = participant.LastName;
                currentParticipant.Phone = participant.Phone;
            }

            var updatedParticipant = await _participantRepository.Update(currentParticipant);
            if (updatedParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {currentParticipant.Id}"));

            if (deposition.Status == DepositionStatus.Confirmed && !string.IsNullOrWhiteSpace(updatedParticipant.Email) && !string.Equals(updatedParticipant.Email, currentParticipantEmail))
                await _depositionEmailService.SendJoinDepositionEmailNotification(deposition, updatedParticipant);

            return Result.Ok(updatedParticipant);
        }

        private Participant HandleEditFullName(Participant participant, Participant currentParticipant, bool emailChanged)
        {
            var hasUser = currentParticipant.User != null;
            if (!hasUser || !emailChanged)
                return currentParticipant;
            
            currentParticipant.Name = string.IsNullOrWhiteSpace(participant.Name) ? currentParticipant.User.FirstName : participant.Name;
            currentParticipant.LastName = string.IsNullOrWhiteSpace(participant.LastName) ? currentParticipant.User.LastName : participant.LastName;
            return currentParticipant;
        }

        private async Task<Participant> ManageEditParticipantPermission(bool emailChanged, Guid depositionId, Participant currentParticipant, Participant editedParticipant)
        {
            if (emailChanged && currentParticipant.User != null)
            {
                await _permissionService.RemoveParticipantPermissions(depositionId, currentParticipant);
                currentParticipant.User = null;
                currentParticipant.UserId = null;
            }

            if (emailChanged && !string.IsNullOrEmpty(editedParticipant.Email))
            {
                var user = await _userService.GetUserByEmail(editedParticipant.Email);
                if (user.IsSuccess)
                {
                    currentParticipant.Phone = user.Value.PhoneNumber;
                    currentParticipant.User = user.Value;
                    currentParticipant.UserId = user.Value.Id;
                    await _permissionService.AddParticipantPermissions(currentParticipant);
                }
            }
            return currentParticipant;
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

        public async Task<Result<Participant>> EditParticipantInDepo(Guid depositionId, Participant participant)
        {
            var notificationType = NotificationEntity.Participant;
            var depositionResult = await _participantValidationHelper.GetValidDepositionForEditParticipantAsync(depositionId);
            if (depositionResult.IsFailed)
                return (Result)depositionResult;

            var targetParticipant = depositionResult.Value.Participants.FirstOrDefault(p => p.Email == participant.Email);
            if (targetParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There are no participant available with email: {participant.Email}."));

            if (targetParticipant.Role != participant.Role)
            {
                var result = _participantValidationHelper.ValidateTargetParticipantForEditRole(depositionResult.Value, participant, targetParticipant);
                if (result.IsFailed)
                    return result;
                notificationType = NotificationEntity.ParticipantRole;
            }

            if (targetParticipant.LastName != participant.LastName || targetParticipant.Name != participant.Name)
            {
                notificationType = notificationType == NotificationEntity.ParticipantRole ? NotificationEntity.Participant : NotificationEntity.ParticipantName;
            }

            var updatedParticipant = await UpdateParticipant(participant, targetParticipant);
            if (updatedParticipant == null)
                return Result.Fail(new ResourceNotFoundError($"There was an error updating Participant with Id: {targetParticipant.Id}"));

            if (updatedParticipant.Role == ParticipantType.Witness)
            {
                await RemoveDummyWitnessParticipantAsync(depositionId);
                await MigratePreviousWitness(depositionResult.Value, updatedParticipant.Id);
            }

            await _permissionService.EditParticipant(updatedParticipant, depositionId);
            var newTwilioToken = await _roomService.RefreshRoomToken(updatedParticipant, depositionResult.Value);
            await NotifyEditionToParticipantAsync(updatedParticipant, newTwilioToken, notificationType);

            _logger.LogInformation("Participant's edition successfully for user {user} on deposition {deposition}", updatedParticipant.User.Id, depositionId);
            return Result.Ok(updatedParticipant);
        }

        private async Task<Participant> UpdateParticipant(Participant participant, Participant targetParticipant)
        {
            targetParticipant.Role = participant.Role;
            targetParticipant.Name = participant.Name;
            targetParticipant.LastName = participant.LastName;
            var updatedParticipant = await _participantRepository.Update(targetParticipant);
            return updatedParticipant;
        }

        private async Task NotifyEditionToParticipantAsync(Participant updatedParticipant, string twilioToken, NotificationEntity notificationType)
        {
            var notificationMessage = new NotificationDto
            {
                EntityType = notificationType,
                Action = NotificationAction.Update,
                Content = new { token = twilioToken, role = updatedParticipant.Role, name = updatedParticipant.GetFullName() }
            };
            await _signalRNotificationManager.SendDirectMessage(updatedParticipant.Email, notificationMessage);
        }

        private async Task RemoveDummyWitnessParticipantAsync(Guid depositionId)
        {
            var dummyWitness = await _participantRepository.GetByFilter(w =>
                w.DepositionId == depositionId &&
                w.Role == ParticipantType.Witness &&
                string.IsNullOrWhiteSpace(w.Email));
            if (dummyWitness?.Any() ?? false)
                await _participantRepository.Remove(dummyWitness.First());
        }

        private async Task MigratePreviousWitness(Deposition deposition, Guid updatedParticipantId)
        {
            var previousWitness = deposition.Participants.FirstOrDefault(p => p.Id != updatedParticipantId && p.Role == ParticipantType.Witness);
            if (previousWitness != null)
            {
                previousWitness.Role = ParticipantType.Observer;
                var updatedPreviousWitness = await _participantRepository.Update(previousWitness);
                await _permissionService.EditParticipant(updatedPreviousWitness, deposition.Id);
                var newTwilioToken = await _roomService.RefreshRoomToken(updatedPreviousWitness, deposition);
                await NotifyEditionToParticipantAsync(updatedPreviousWitness, newTwilioToken, NotificationEntity.ParticipantRole);
            }
        }
    }
}