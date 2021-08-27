using System;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Linq;

namespace PrecisionReporters.Platform.Domain.Mappers
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
                CreationDate = new DateTimeOffset(model.CreationDate, TimeSpan.Zero),
                CompanyName = model.CompanyName,
                CompanyAddress = model.CompanyAddress,
                IsAdmin = model.IsAdmin,
                IsGuest = model.IsGuest,
                VerificationDate = model.VerifiedUsers?.FirstOrDefault(y => y.VerificationType == VerificationType.VerifyUser)?.VerificationDate ?? Convert.ToDateTime("01/01/1971")
            };
        }

        public User ToModel(UserDto dto)
        {
            return new User
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailAddress = dto.EmailAddress.ToLower(),
                PhoneNumber = dto.PhoneNumber,
                CreationDate = dto.CreationDate.UtcDateTime,
                CompanyName = dto.CompanyName,
                CompanyAddress = dto.CompanyAddress
            };
        }

        public User ToModel(CreateUserDto dto)
        {
            return new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailAddress = dto.EmailAddress.ToLower(),
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password,
                CompanyName = dto.CompanyName,
                CompanyAddress = dto.CompanyAddress
            };
        }
    }
}
