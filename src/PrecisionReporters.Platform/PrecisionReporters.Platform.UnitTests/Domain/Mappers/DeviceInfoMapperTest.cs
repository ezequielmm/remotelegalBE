using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DeviceInfoMapperTest
    {
        private readonly DeviceInfoMapper _deviceInfoMapper;

        public DeviceInfoMapperTest()
        {
            _deviceInfoMapper = new DeviceInfoMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithDeviceInfoDto()
        {
            // Arrange
            var dto = new DeviceInfoDto
            {
                Camera = new CameraDto
                {
                    Name = "cam1",
                    Status = CameraStatus.Enabled
                },
                Microphone = new MicrophoneDto
                {
                    Name = "mic1",
                },
                Speakers = new SpeakersDto
                {
                    Name = "speaker1"
                }
            };

            // Act
            var result = _deviceInfoMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Camera.Name, result.CameraName);
            Assert.Equal(dto.Camera.Status, result.CameraStatus);
            Assert.Equal(System.DateTime.UtcNow, result.CreationDate, TimeSpan.FromSeconds(5));
            Assert.Equal(dto.Microphone.Name, result.MicrophoneName);
            Assert.Equal(dto.Speakers.Name, result.SpeakersName);
        }
    }
}
