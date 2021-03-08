using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Linq;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class BreakRoomMapper : IMapper<BreakRoom, BreakRoomDto, object>
    {
        public BreakRoomDto ToDto(BreakRoom model)
        {
            return new BreakRoomDto
            {
                Id = model.Id,
                Name = model.Name,
                IsLocked = model.IsLocked,
                CurrentAttendes = model.Attendees?.Select(p => new UserOutputDto(p.User)).ToList(),
            };
        }

        public BreakRoom ToModel(BreakRoomDto dto)
        {
            throw new NotImplementedException();
        }

        public BreakRoom ToModel(object dto)
        {
            throw new NotImplementedException();
        }
    }
}
