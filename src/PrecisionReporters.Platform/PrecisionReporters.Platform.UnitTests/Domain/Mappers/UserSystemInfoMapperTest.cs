using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class UserSystemInfoMapperTest
    {
        private readonly UserSystemInfoMapper _userSystemInfoMapper;

        public UserSystemInfoMapperTest()
        {
            _userSystemInfoMapper = new UserSystemInfoMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithUserSystemInfoDto()
        {
            // Arrange
            var dto = new UserSystemInfoDto
            {
                OS = "Windows",
                Browser = "Firefox",
                Device = "PC"
            };

            // Act
            var result = _userSystemInfoMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.OS, result.OS);
            Assert.Equal(dto.Browser, result.Browser);
            Assert.Equal(dto.Device, result.Device);

        }
    }
}
