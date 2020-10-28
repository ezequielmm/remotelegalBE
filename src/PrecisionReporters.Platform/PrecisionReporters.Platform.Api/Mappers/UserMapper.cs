using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class UserMapper : IMapper<User, UserDto, CreateUserDto>
    {
        public UserDto ToDto(User model)
        {
            return new UserDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailAddress = model.EmailAddress,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                CreationDate = model.CreationDate
            };
        }

        public User ToModel(UserDto dto)
        {
            return new User
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailAddress = dto.EmailAddress,
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password,
                CreationDate = dto.CreationDate
            };
        }

        public User ToModel(CreateUserDto dto)
        {
            return new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailAddress = dto.EmailAddress,
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password
            };
        }
    }
}
