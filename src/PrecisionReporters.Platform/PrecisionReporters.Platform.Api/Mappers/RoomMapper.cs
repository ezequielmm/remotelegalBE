using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class RoomMapper : IMapper<Room, RoomDto, CreateRoomDto>
    {
        public RoomDto ToDto(Room model)
        {
            return new RoomDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Name = model.Name
            };
        }

        public Room ToModel(RoomDto roomDto)
        {
            return new Room
            {
                Name = roomDto.Name
            };
        }

        public Room ToModel(CreateRoomDto dto)
        {
            return new Room
            {
                Name = dto.Name
            };
        }
    }
}
