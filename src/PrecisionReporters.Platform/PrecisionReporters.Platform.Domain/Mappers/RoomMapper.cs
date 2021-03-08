using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class RoomMapper : IMapper<Room, RoomDto, CreateRoomDto>
    {
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;

        public RoomMapper(IMapper<Composition, CompositionDto, CallbackCompositionDto> compositionMapper)
        {
            _compositionMapper = compositionMapper;
        }

        public RoomDto ToDto(Room model)
        {
            return new RoomDto
            {
                Id = model.Id,
                SId = model.SId,
                CreationDate = model.CreationDate,
                Name = model.Name,
                IsRecordingEnabled = model.IsRecordingEnabled,
                Status = model.Status.ToString(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Composition = (model.Composition != null) ? _compositionMapper.ToDto(model.Composition) : null
            };
        }

        public Room ToModel(RoomDto dto)
        {
            return new Room
            {
                Name = dto.Name,
                IsRecordingEnabled = dto.IsRecordingEnabled
            };
        }

        public Room ToModel(CreateRoomDto dto)
        {
            return new Room
            {
                Name = dto.Name,
                IsRecordingEnabled = dto.IsRecordingEnabled
            };
        }
    }
}
