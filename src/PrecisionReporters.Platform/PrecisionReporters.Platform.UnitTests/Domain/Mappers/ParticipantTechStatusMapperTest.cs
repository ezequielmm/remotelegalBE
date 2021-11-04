using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class ParticipantTechStatusMapperTest
    {
        private readonly ParticipantTechStatusMapper _participantTechStatusMapper;

        public ParticipantTechStatusMapperTest()
        {
            _participantTechStatusMapper = new ParticipantTechStatusMapper();
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
                LastName = "lastName",
                Phone = "5555555555",
                Role = ParticipantType.CourtReporter,
                IsMuted = false,
                IsAdmitted = true,
                HasJoined = true,
                User = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "first",
                    LastName = "last",
                    EmailAddress = "email@test.com",
                    ActivityHistories = null
                },
                DeviceInfo = new DeviceInfo {
                    CameraName = "cam1",
                    CameraStatus = CameraStatus.Enabled,
                    CreationDate = It.IsAny<DateTime>(),
                    Id = Guid.NewGuid(),
                    MicrophoneName = "mic1",
                    SpeakersName = "speaker1"
                }
            };

            // Act
            var result = _participantTechStatusMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Email, result.Email);
            Assert.Equal($"{model.Name} {model.LastName}", result.Name);
            Assert.Equal(model.Role.ToString(), result.Role);
            Assert.Equal(model.DeviceInfo.CameraName, result.Devices.Camera.Name);
            Assert.Equal(model.DeviceInfo.CameraStatus, result.Devices.Camera.Status);
            Assert.Equal(model.DeviceInfo.MicrophoneName, result.Devices.Microphone.Name);
            Assert.Equal(model.DeviceInfo.SpeakersName, result.Devices.Speakers.Name);
        }

        [Fact]
        public void ToDto_ShouldReturnDto_AfterFirstEntrance()
        {
            // Arrange
            var browser = "Firefox";
            var ipAddress = "1.0.0.0";
            var operatingSystem = "Windows";
            var device = "PC";

            var model = new Participant
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Email = "email@test.com",
                Name = "name",
                LastName = null,
                Phone = "5555555555",
                Role = ParticipantType.CourtReporter,
                IsMuted = false,
                IsAdmitted = true,
                HasJoined = true,
                User = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "first",
                    LastName = "last",
                    EmailAddress = "email@test.com",
                    ActivityHistories = new List<ActivityHistory>
                    {
                        new ActivityHistory
                        {
                            Action = ActivityHistoryAction.SetSystemInfo,
                            ActivityDate = It.IsAny<DateTime>(),
                            Browser = browser,
                            IPAddress = ipAddress,
                            OperatingSystem = operatingSystem,
                            Device = device
                        }
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    CameraName = "cam1",
                    CameraStatus = CameraStatus.Enabled,
                    CreationDate = It.IsAny<DateTime>(),
                    Id = Guid.NewGuid(),
                    MicrophoneName = "mic1",
                    SpeakersName = "speaker1"
                }
            };

            // Act
            var result = _participantTechStatusMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Email, result.Email);
            Assert.Equal($"{model.Name} {model.LastName}", result.Name);
            Assert.Equal(model.Role.ToString(), result.Role);
            Assert.Equal(browser, result.Browser);
            Assert.Equal(device, result.Device);
            Assert.Equal(operatingSystem, result.OperatingSystem);
            Assert.Equal(ipAddress, result.IP);
            Assert.Equal(model.DeviceInfo.CameraName, result.Devices.Camera.Name);
            Assert.Equal(model.DeviceInfo.CameraStatus, result.Devices.Camera.Status);
            Assert.Equal(model.DeviceInfo.MicrophoneName, result.Devices.Microphone.Name);
            Assert.Equal(model.DeviceInfo.SpeakersName, result.Devices.Speakers.Name);
        }
    }
}
