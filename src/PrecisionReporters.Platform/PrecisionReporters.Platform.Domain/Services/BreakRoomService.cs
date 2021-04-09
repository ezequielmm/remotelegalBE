using System;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class BreakRoomService : IBreakRoomService
    {
        private readonly IBreakRoomRepository _breakRoomRepository;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;

        public BreakRoomService(IBreakRoomRepository breakRoomRepository, IUserService userService, IRoomService roomService)
        {
            _breakRoomRepository = breakRoomRepository;
            _userService = userService;
            _roomService = roomService;
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

            if (breakRoom.IsLocked)
                return Result.Fail(new InvalidInputError($"The Break Room [{breakRoom.Name}] is currently locked."));

            var role = currentParticipant?.Role ?? ParticipantType.Observer;

            // TODO: Add distributed lock when our infra allows it
            if (breakRoom.Room.Status == RoomStatus.Created)
            {
                await _roomService.StartRoom(breakRoom.Room, true);
            }
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
    }
}
