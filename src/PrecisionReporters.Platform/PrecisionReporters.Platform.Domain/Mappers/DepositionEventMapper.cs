using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class DepositionEventMapper : IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto>
    {
        public DepositionEventMapper()
        {
        }

        public DepositionEventDto ToDto(DepositionEvent model)
        {
            return new DepositionEventDto
            {
                Id = model.Id,
                CreationDate = new DateTimeOffset(model.CreationDate, TimeSpan.Zero),
                EventType = model.EventType,
                Details = model.Details
            };
        }

        public DepositionEvent ToModel(DepositionEventDto dto)
        {
            throw new NotImplementedException();
        }

        public DepositionEvent ToModel(CreateDepositionEventDto dto)
        {
            return new DepositionEvent
            {
                EventType = dto.EventType,
                Details = dto.Details,
            };
        }
    }
}
