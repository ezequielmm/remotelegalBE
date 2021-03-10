using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class ParticipantMapperTests
    {
        private readonly ParticipantMapper _participantMapper;

        public ParticipantMapperTests()
        {
            _participantMapper = new ParticipantMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithParticipantDto()
        {
            // Arrange
            var dto = new ParticipantDto
            {
                CreationDate = DateTimeOffset.Now,
                Email = "TestEmail@PascalCase.Com",
                Name = "Test",
                Phone = "5555555555",
                Role = "Attorney",
                Id = Guid.NewGuid(),
                User = new UserOutputDto { Id = Guid.NewGuid() }
            };

            // Act
            var result = _participantMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.CreationDate.ToLocalTime(), result.CreationDate);
            Assert.Equal(dto.Email.ToLower(), result.Email);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Phone, result.Phone);
            Assert.Equal(dto.Role, result.Role.ToString());
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.User.Id, result.UserId);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateParticipantDto()
        {
            // Arrange
            var dto = new CreateParticipantDto
            {
                Email = "TestEmail@PascalCase.Com",
                Name = "Test",
                Phone = "5555555555",
                Role = ParticipantType.Attorney
            };

            // Act
            var result = _participantMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Email.ToLower(), result.Email);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Phone, result.Phone);
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var model = new Participant
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Email = "email@test.com",
                Name = "name",
                Phone = "5555555555",
                Role = ParticipantType.CourtReporter,
                User = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "first",
                    LastName = "last",
                    EmailAddress = "email@test.com"
                }                
            };

            // Act
            var result = _participantMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Email, result.Email);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.Phone, result.Phone);
            Assert.Equal(model.Role.ToString(), result.Role);
            Assert.Equal(model.User.Id, result.User.Id);
            Assert.Equal(model.User.FirstName, result.User.FirstName);
            Assert.Equal(model.User.LastName, result.User.LastName);
            Assert.Equal(model.User.EmailAddress, result.User.EmailAddress);
        }
    }
}
