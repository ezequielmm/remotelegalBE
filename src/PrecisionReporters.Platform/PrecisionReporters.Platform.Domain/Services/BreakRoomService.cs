using FluentResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class BreakRoomService : IBreakRoomService
    {
        private readonly IBreakRoomRepository _breakRoomRepository;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;
        private readonly IMapper<BreakRoom, BreakRoomDto, object> _breakRoomMapper;
        private readonly ISignalRDepositionManager _signalRNotificationManager;
        private JsonSerializerSettings _serializeOptions;

        public BreakRoomService(IBreakRoomRepository breakRoomRepository, IUserService userService, IRoomService roomService, IMapper<BreakRoom, BreakRoomDto, object> breakRoomMapper, ISignalRDepositionManager signalRNotificationManager)
        {
            _breakRoomRepository = breakRoomRepository;
            _userService = userService;
            _roomService = roomService;
            _serializeOptions = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _breakRoomMapper = breakRoomMapper;
            _signalRNotificationManager = signalRNotificationManager;
        }

        public async Task<Result<BreakRoom>> GetBreakRoomById(Guid id, string[] include = null)
        {
            var breakRoom = await _breakRoomRepository.GetById(id, include);
            if (breakRoom == null)
                return Result.Fail(new ResourceNotFoundError($"Break Room with Id {id} could not be found"));

            return Result.Ok(breakRoom);
        }

        public async Task<Result<string>> JoinBreakRoom(Guid id, Participant currentParticipant)
        {
            var breakRoomResult = await GetBreakRoomById(id, new[] { nameof(BreakRoom.Room), $"{nameof(BreakRoom.Attendees)}.{nameof(BreakRoomAttendee.User)}" });
            if (breakRoomResult.IsFailed)
                return breakRoomResult.ToResult<string>();

            var breakRoom = breakRoomResult.Value;

            var role = currentParticipant?.Role ?? ParticipantType.Observer;

            if (breakRoom.IsLocked && role != ParticipantType.CourtReporter && !breakRoom.Attendees.Any(a => a.UserId == currentParticipant.UserId))
                return Result.Fail(new InvalidInputError($"The Break Room [{breakRoom.Name}] is currently locked."));

            // TODO: Add distributed lock when our infra allows it
            if (breakRoom.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(breakRoom.Room, true);
            }

            var existingRooms = await _roomService.GetTwilioRoomByNameAndStatus(breakRoom.Room.Name, RoomStatusEnum.InProgress);
            if (!existingRooms.Any())
                await _roomService.StartRoom(breakRoom.Room, true);

            var token = await _roomService.GenerateRoomToken(breakRoom.Room.Name, currentParticipant.User, role, currentParticipant.User.EmailAddress);
            if (token.IsFailed)
                return token.ToResult<string>();

            await AddAttendeeToBreakRoom(breakRoom, currentParticipant.User);

            return token;
        }

        public async Task<Result<BreakRoom>> LockBreakRoom(Guid breakRoomId, bool lockRoom)
        {
            var breakRoomResult = await GetBreakRoomById(breakRoomId);
            if (breakRoomResult.IsFailed)
                return breakRoomResult.ToResult<BreakRoom>();

            var breakRoom = breakRoomResult.Value;

            if (breakRoom.IsLocked == lockRoom)
                return Result.Fail(new InvalidInputError($"Unable to change lock state to the BreakRoom [{breakRoom.Name}] current state IsLocked = {breakRoom.IsLocked}."));

            breakRoom.IsLocked = lockRoom;
            var breakRoomUpdated = await _breakRoomRepository.Update(breakRoom);

            return Result.Ok(breakRoomUpdated);
        }

        private async Task AddAttendeeToBreakRoom(BreakRoom breakRoom, User user)
        {
            if (!breakRoom.Attendees.Any(x => x.User.Id == user.Id))
            {
                breakRoom.Attendees.Add(new BreakRoomAttendee { User = user, BreakRoom = breakRoom });
                await _breakRoomRepository.Update(breakRoom);
            }
        }

        public async Task<Result<BreakRoom>> GetByRoomId(Guid roomId)
        {
            var includes = new[] { nameof(BreakRoom.Attendees) };
            var result = await _breakRoomRepository.GetFirstOrDefaultByFilter(b => b.RoomId == roomId, includes);
            if (result != null)
                return Result.Ok(result);
            else
                return Result.Fail(new ResourceNotFoundError($"BreakRoom not found with Room ID: {roomId}"));
        }

        public async Task<Result<BreakRoom>> RemoveAttendeeCallback(BreakRoom breakRoom, string userIdentity)
        {
            //TODO: Build a method for Serialize and Deserialize Twilio Identity which we can use wherever, like a extension method
            var identity = (TwilioIdentity)JsonConvert.DeserializeObject(userIdentity, typeof(TwilioIdentity), _serializeOptions);
            var user = await _userService.GetUserByEmail(identity.Email);

            if (user.IsFailed)
                return Result.Fail(new ResourceNotFoundError($"User not found with email: {identity.Email}"));

            var sendNotification = false;
            var attendee = breakRoom.Attendees.FirstOrDefault(a => a.UserId == user.Value.Id);

            if (attendee == null)
                return Result.Fail(new ResourceNotFoundError($"Attendee not found with User Id: {user.Value.Id}"));

            breakRoom.Attendees.Remove(attendee);
            if (!breakRoom.Attendees.Any() && breakRoom.IsLocked)
            {
                breakRoom.IsLocked = false;
                sendNotification = true;
            }
            await _breakRoomRepository.Update(breakRoom);

            if (sendNotification)
                await SendUnlockBreakRoomNotification(breakRoom);

            return Result.Ok();
        }

        private async Task SendUnlockBreakRoomNotification(BreakRoom breakRoom)
        {
            var notificationtDto = new NotificationDto
            {
                Action = NotificationAction.Update,
                EntityType = NotificationEntity.LockBreakRoom,
                Content = _breakRoomMapper.ToDto(breakRoom)
            };
            await _signalRNotificationManager.SendNotificationToDepositionMembers(breakRoom.DepositionId, notificationtDto);
        }
    }
}
