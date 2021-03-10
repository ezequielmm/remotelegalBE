using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class UserMapperTests
    {
        private readonly UserMapper _userMapper;

        public UserMapperTests()
        {
            _userMapper = new UserMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithUserDto()
        {
            // Arrange
            var dto = new UserDto
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                EmailAddress = "TestEmail@PascalCase.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "5555555555",
                CompanyAddress = "742 Evergreen Terrace",
                CompanyName = "Simpsons & Co",
                IsAdmin = false,
                IsGuest = false
            };

            // Act
            var result = _userMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate, result.CreationDate);
            Assert.Equal(dto.EmailAddress.ToLower(), result.EmailAddress);
            Assert.Equal(dto.FirstName, result.FirstName);
            Assert.Equal(dto.LastName, result.LastName);
            Assert.Equal(dto.PhoneNumber, result.PhoneNumber);
            Assert.Equal(dto.CompanyAddress, result.CompanyAddress);
            Assert.Equal(dto.CompanyName, result.CompanyName);
            Assert.Equal(dto.IsAdmin, result.IsAdmin);
            Assert.Equal(dto.IsGuest, result.IsGuest);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateUserDto()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                EmailAddress = "TestEmail@PascalCase.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "5555555555",
                CompanyAddress = "742 Evergreen Terrace",
                CompanyName = "Simpsons & Co",
            };

            // Act
            var result = _userMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.EmailAddress.ToLower(), result.EmailAddress);
            Assert.Equal(dto.FirstName, result.FirstName);
            Assert.Equal(dto.LastName, result.LastName);
            Assert.Equal(dto.PhoneNumber, result.PhoneNumber);
            Assert.Equal(dto.CompanyAddress, result.CompanyAddress);
            Assert.Equal(dto.CompanyName, result.CompanyName);
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var model = new User
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                EmailAddress = "test@test.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "5555555555",
                CompanyAddress = "742 Evergreen Terrace",
                CompanyName = "Simpsons & Co",
                IsAdmin = false,
                IsGuest = false
            };

            // Act
            var result = _userMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.EmailAddress, result.EmailAddress);
            Assert.Equal(model.FirstName, result.FirstName);
            Assert.Equal(model.LastName, result.LastName);
            Assert.Equal(model.PhoneNumber, result.PhoneNumber);
            Assert.Equal(model.CompanyAddress, result.CompanyAddress);
            Assert.Equal(model.CompanyName, result.CompanyName);
            Assert.Equal(model.IsAdmin, result.IsAdmin);
            Assert.Equal(model.IsGuest, result.IsGuest);
        }
    }
}
