using System;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DepositionEventMapper : IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto>
    {
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public DepositionEventMapper(IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _userMapper = userMapper;
        }

        public DepositionEventDto ToDto(DepositionEvent model)
        {
            return new DepositionEventDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                EventType = model.EventType,
                User = _userMapper.ToDto(model.User),
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
                Details = dto.Details
            };
        }
    }
}
