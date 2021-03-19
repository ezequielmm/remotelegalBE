using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly IParticipantRepository _participantRepository;
        private readonly ISignalRNotificationManager _signalRNotificationManager;
        private readonly IUserService _userService;

        public ParticipantService(IParticipantRepository participantRepository, ISignalRNotificationManager signalRNotificationManager, IUserService userService)
        {
            _participantRepository = participantRepository;
            _signalRNotificationManager = signalRNotificationManager;
            _userService = userService;
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
                        return Result.Fail(new ResourceNotFoundError("The are no participant available with such userId and depositionId combination."));
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
                        return Result.Fail(new UnexpectedError("The was an error creating a new Participant."));
                }
                else
                {
                    participant.IsMuted = participantStatusDto.IsMuted;
                    var updatedParticipant = await _participantRepository.Update(participant);
                    if (updatedParticipant == null)
                        return Result.Fail(new ResourceNotFoundError($"The was an error updating Participant with Id: {participant.Id}"));
                }

                participantStatusDto.Email = user.EmailAddress;
                var notificationDto = new NotificationDto
                {
                    Action = NotificationAction.Update,
                    EntityType = NotificationEntity.ParticipantStatus,
                    Content = participantStatusDto
                };

                await _signalRNotificationManager.SendNotificationToGroupMembers(depositionId, notificationDto);

                return Result.Ok(participantStatusDto);
            }
            catch (Exception ex)
            {
                return Result.Fail(new UnexpectedError(ex.Message));
            }
        }
    }
}