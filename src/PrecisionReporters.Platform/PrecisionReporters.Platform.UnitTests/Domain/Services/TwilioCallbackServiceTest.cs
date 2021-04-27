using System;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class TwilioCallbackServiceTest : IDisposable
    {
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<ILogger<TwilioCallbackService>> _loggerMock;
        private readonly Mock<IBreakRoomService> _breakRoomServiceMock;

        private TwilioCallbackService _service;

        public TwilioCallbackServiceTest()
        {
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<TwilioCallbackService>>();
            _breakRoomServiceMock = new Mock<IBreakRoomService>();
            _service = new TwilioCallbackService(_depositionServiceMock.Object,
                _roomServiceMock.Object, _loggerMock.Object, _breakRoomServiceMock.Object);
        }

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldSet_RecordingStartDate_ForRecordingCompletedEvent()
        {
            var eventDto = new RoomCallbackDto
            {
                Duration = 10,
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "recording-started",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Once);
        }


        [Fact]
        public async Task UpdateStatusCallback_ShouldNotSet_RecordingStartDate_ForRecordingCompletedEventAndStartedReferenceDifferent()
        {
            var eventDto = new RoomCallbackDto
            {
                Duration = 10,
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "recording-completed",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldSet_StartedReferenceIfIsNull_ForParticipantConnected()
        {
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "participant-connected",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsFailed);
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldNotSet_StartedReferenceIfIsNotNull_ForParticipantConnected()
        {
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "participant-connected",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldEndDeposition_ForRoomEndedIFDepositionIsNotCompleted()
        {
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "room-ended",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            var depostion = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            depostion.Status = DepositionStatus.Confirmed;

            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));
            _depositionServiceMock.Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(depostion));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _roomServiceMock.Verify(x => x.CreateComposition(It.IsAny<Room>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldNotEndDeposition_ForRoomEndedIFDepositionIsCompleted()
        {
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "room-ended",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            var depostion = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            depostion.Room.Status = RoomStatus.Completed;

            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));
            _depositionServiceMock.Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(depostion));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _depositionServiceMock.Verify(x => x.EndDeposition(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldReturnOk_IfEventDoesNotMatch()
        {
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "foo-event"
            };

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
        }
    }
}