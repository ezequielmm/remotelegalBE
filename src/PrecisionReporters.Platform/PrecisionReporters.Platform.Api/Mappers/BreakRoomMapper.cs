using System;
using System.Linq;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
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
