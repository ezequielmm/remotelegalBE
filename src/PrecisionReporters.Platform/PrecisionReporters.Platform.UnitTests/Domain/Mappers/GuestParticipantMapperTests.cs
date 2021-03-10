using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class GuestParticipantMapperTests
    {
        private readonly GuestParticipantMapper _guestParticipantMapper;

        public GuestParticipantMapperTests()
        {
            _guestParticipantMapper = new GuestParticipantMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithAddParticipantDto()
        {
            // Arrange
            var dto = new AddParticipantDto
            {
                EmailAddress = "TestEmail@PascalCase.Com",
                ParticipantType = ParticipantType.CourtReporter
            };

            // Act
            var result = _guestParticipantMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.EmailAddress.ToLower(), result.Email);
            Assert.Equal(dto.ParticipantType, result.Role);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateGuestDto()
        {
            // Arrange
            var dto = new CreateGuestDto
            {
                EmailAddress = "TestEmail@PascalCase.Com",
                ParticipantType = ParticipantType.CourtReporter,
                Name = "Test"
            };

            // Act
            var result = _guestParticipantMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.EmailAddress.ToLower(), result.Email);
            Assert.Equal(dto.ParticipantType, result.Role);
            Assert.Equal(dto.Name, result.Name);
        }
    }
}
