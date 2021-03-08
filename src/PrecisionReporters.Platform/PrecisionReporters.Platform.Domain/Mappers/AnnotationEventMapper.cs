using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class AnnotationEventMapper : IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto>
    {
        public AnnotationEventDto ToDto(AnnotationEvent model)
        {
            return new AnnotationEventDto
            {
                Id = model.Id,
                CreationDate = new DateTimeOffset(model.CreationDate, TimeSpan.Zero),
                Action = model.Action,
                Author = new UserOutputDto(model.Author),
                Details = model.Details
            };
        }

        public AnnotationEvent ToModel(AnnotationEventDto dto)
        {
            throw new NotImplementedException();
        }

        public AnnotationEvent ToModel(CreateAnnotationEventDto dto)
        {
            return new AnnotationEvent
            {
                Action = dto.Action,
                Details = dto.Details
            };
        }
    }
}
