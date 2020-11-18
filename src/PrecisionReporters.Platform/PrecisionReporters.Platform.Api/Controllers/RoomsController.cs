using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IMapper<Room, RoomDto, CreateRoomDto> _roomMapper;

        public RoomsController(IRoomService roomService,
            IMapper<Room, RoomDto, CreateRoomDto> roomMapper)
        {
            _roomService = roomService;
            _roomMapper = roomMapper;
        }

        /// <summary>
        /// Creates a new video conference room with a specific name
        /// </summary>
        /// <param name="roomDto">Room to create.</param>
        /// <returns>Newly created room</returns>
        [HttpPost]
        public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto roomDto)
        {
            var getRoomResult = await _roomService.GetByName(roomDto.Name);
            if (getRoomResult.HasError<ResourceNotFoundError>())
            {
                var createRoomResult = await _roomService.Create(_roomMapper.ToModel(roomDto));
                if (createRoomResult.IsFailed)
                    return WebApiResponses.GetErrorResponse(createRoomResult);

                var createdRoom = _roomMapper.ToDto(createRoomResult.Value);

                return Created(nameof(GetRoom), createdRoom);
            }
            else if (getRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getRoomResult);

            return Created(nameof(GetRoom), getRoomResult.Value);

        }

        /// <summary>
        /// Get a certain room with a specific name
        /// </summary>
        /// <param name="name">Room to get or create.</param>
        /// <returns>A room with provided name</returns>
        [HttpGet("{name}")]
        public async Task<ActionResult<RoomDto>> GetRoom(string name)
        {
            var getRoomResult = await _roomService.GetByName(name);
            if (getRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getRoomResult);

            return Ok(_roomMapper.ToDto(getRoomResult.Value));
        }

        /// <summary>
        /// Creates a Token to join a specific room
        /// </summary>
        /// <param name="roomDto">Room to create token for.</param>
        /// <returns>A JWT token with specific grants to join to a room</returns>
        [Route("token")]
        [HttpPost]
        public async Task<ActionResult<RoomTokenDto>> GenerateRoomToken(CreateRoomDto roomDto)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var tokenOperationResult = await _roomService.GenerateRoomToken(roomDto.Name, identity);
            if (tokenOperationResult.IsFailed)
                return WebApiResponses.GetErrorResponse(tokenOperationResult);

            var tokenDto = new RoomTokenDto { Token = tokenOperationResult.Value };
            return Ok(tokenDto);
        }

        /// <summary>
        /// End a room with a specific name
        /// </summary>
        /// <param name="roomDto">Room to end.</param>
        /// <returns>A room with provided name</returns>
        [Route("endRoom")]
        [HttpPost]
        public async Task<ActionResult<RoomDto>> EndRoom(RoomDto roomDto)
        {
            var getRoomResult = await _roomService.GetByName(roomDto.Name);
            if (getRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getRoomResult);

            var endRoomResult = await _roomService.EndRoom(getRoomResult.Value);
            if (endRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(endRoomResult);

            return Ok(_roomMapper.ToDto(endRoomResult.Value));
        }

        /// <summary>
        /// Start a room with a specific name
        /// </summary>
        /// <param name="roomDto">Room to start.</param>
        /// <returns>A room with provided name</returns>
        [Route("startRoom")]
        [HttpPost]
        public async Task<ActionResult<RoomDto>> StartRoom(RoomDto roomDto)
        {
            var getRoomResult = await _roomService.GetByName(roomDto.Name);
            if (getRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getRoomResult);

            var startRoomResult = await _roomService.StartRoom(getRoomResult.Value);
            if (startRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(startRoomResult);

            return Ok(_roomMapper.ToDto(getRoomResult.Value));
        }
    }
}
