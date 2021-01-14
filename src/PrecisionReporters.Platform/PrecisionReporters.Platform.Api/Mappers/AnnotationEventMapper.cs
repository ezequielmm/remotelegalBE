using System;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class AnnotationEventMapper : IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto>
    {
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public AnnotationEventMapper(IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _userMapper = userMapper;
        }

        public AnnotationEventDto ToDto(AnnotationEvent model)
        {
            return new AnnotationEventDto
            {
                Id = model.Id,
                CreationDate = new DateTimeOffset(model.CreationDate, TimeSpan.Zero),
                Action = model.Action,
                Author = _userMapper.ToDto(model.Author),
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
