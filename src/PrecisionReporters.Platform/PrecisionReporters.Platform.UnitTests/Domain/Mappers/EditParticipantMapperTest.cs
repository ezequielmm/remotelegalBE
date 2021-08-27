using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class EditParticipantMapperTest
    {
        private readonly EditParticipantMapper _editParticipantMapper;

        public EditParticipantMapperTest()
        {
            _editParticipantMapper = new EditParticipantMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithEditParticipantDto()
        {
            // Arrange
            var dto = new EditParticipantDto
            {
                Id = Guid.NewGuid(),
                Email = "mail@mock.com",
                Name = "John Doe",
                Phone = "2233222333",
                Role = Platform.Data.Enums.ParticipantType.Attorney
            };

            // Act
            var result = _editParticipantMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Phone, result.Phone);
            Assert.Equal(dto.Role, result.Role);
            Assert.Equal(dto.Email, result.Email);
        }
    }
}
