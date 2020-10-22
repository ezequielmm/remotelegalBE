using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        /// <param name="roomDto"></param>
        /// <returns>Newly created room</returns>
        [HttpPost]
        public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto roomDto)
        {
            var room = await _roomService.GetByName(roomDto.Name);
            if (room == null)
            {
                room = await _roomService.Create(_roomMapper.ToModel(roomDto));
            }

            return Created(nameof(GetRoom), _roomMapper.ToDto(room));
        }

        /// <summary>
        /// Get a certain room with a specific name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A room with provided name</returns>
        [HttpGet("{name}")]
        public async Task<ActionResult<RoomDto>> GetRoom(string name)
        {
            var room = await _roomService.GetByName(name);
            if (room == null)
            {
                return NotFound();
            }

            return Ok(_roomMapper.ToDto(room));
        }

        /// <summary>
        /// Creates a Token to join a specific room
        /// </summary>
        /// <param name="roomDto"></param>
        /// <returns>A JWT token with specific grants to join to a room</returns>
        [Route("token")]
        [HttpPost]
        public ActionResult<RoomTokenDto> GenerateRoomToken(RoomDto roomDto)
        {
            var token = _roomService.GenerateRoomToken(roomDto.Name);
            var tokenDto = new RoomTokenDto { Token = token };
            return Ok(tokenDto);
        }
    }
}
