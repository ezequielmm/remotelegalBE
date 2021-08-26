using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class VerifyUserMapperTest
    {
        private readonly VerifyUserMapper _verifyUserMapper;

        public VerifyUserMapperTest()
        {
            _verifyUserMapper = new VerifyUserMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateVerifyUserDto()
        {
            // Arrange
            var dto = new CreateVerifyUserDto
            {
                UserId = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                IsUsed = true,
                VerificationDate = It.IsAny<DateTime>()
            };

            // Act
            var result = _verifyUserMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.UserId, result.User.Id);
            Assert.Equal(dto.CreationDate.ToLocalTime(), result.CreationDate);
            Assert.Equal(dto.IsUsed, result.IsUsed);
        }
    }
}
