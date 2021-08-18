using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
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
        private readonly Mock<ITwilioService> _twilioServiceMock;
        private readonly Mock<ITwilioParticipantRepository> _twilioParticipantRepositoryMock;

        private TwilioCallbackService _service;

        public TwilioCallbackServiceTest()
        {
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<TwilioCallbackService>>();
            _breakRoomServiceMock = new Mock<IBreakRoomService>();
            _twilioServiceMock = new Mock<ITwilioService>();
            _twilioParticipantRepositoryMock = new Mock<ITwilioParticipantRepository>();
            _service = new TwilioCallbackService(_depositionServiceMock.Object,
                _roomServiceMock.Object, _loggerMock.Object, _breakRoomServiceMock.Object,
                _twilioServiceMock.Object,
                _twilioParticipantRepositoryMock.Object);
        }

        public void Dispose()
        {
            // Tear down
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
        public async Task UpdateStatusCallback_ShouldOk_DepositionNotFound()
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
            _depositionServiceMock.Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Fail(new Error()));
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldOk_ParticipantNotFound()
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
            var depostion = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _depositionServiceMock.Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(depostion));
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            _roomServiceMock.Verify(x => x.Update(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldOk_TwilioParticipant_Created()
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
            var depostion = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _twilioServiceMock.Setup(x => x.DeserializeObject(It.IsAny<string>())).Returns(new TwilioIdentity { Email = "witness@email.com" });
            _depositionServiceMock.Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(depostion));
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));
            _twilioParticipantRepositoryMock.Setup(x => x.Create(It.IsAny<TwilioParticipant>())).ReturnsAsync(new TwilioParticipant());

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
            _roomServiceMock.Verify(x => x.CreateComposition(It.IsAny<Room>(), It.IsAny<string>(),It.IsAny<Guid>()), Times.Once);
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

        [Fact]
        public async Task UpdateStatusCallback_ShouldOk_Removed_BreakRoomAttendee()
        {
            //Arrange
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "participant-disconnected",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));
            _breakRoomServiceMock.Setup(x => x.GetByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok());
            _twilioParticipantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<TwilioParticipant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync((TwilioParticipant)null);

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _breakRoomServiceMock.Verify(x => x.RemoveAttendeeCallback(It.IsAny<BreakRoom>(), It.IsAny<string>()), Times.Once);
            _breakRoomServiceMock.Verify(x => x.GetByRoomId(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusCallback_ShouldOk_Update_TwilioParticipant()
        {
            //Arrange
            var eventDto = new RoomCallbackDto
            {
                Timestamp = DateTime.UtcNow,
                StatusCallbackEvent = "participant-disconnected",
                ParticipantSid = "P01",
                RoomSid = "R01"
            };

            var roomId = Guid.NewGuid();

            var room = RoomFactory.GetRoomById(roomId);
            _roomServiceMock.Setup(x => x.GetRoomBySId(It.IsAny<string>())).ReturnsAsync(Result.Ok(room));
            _breakRoomServiceMock.Setup(x => x.GetByRoomId(It.IsAny<Guid>())).ReturnsAsync(Result.Fail(new Error()));
            _twilioParticipantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<TwilioParticipant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(new TwilioParticipant());

            // Act
            var result = await _service.UpdateStatusCallback(eventDto);

            // Assert
            Assert.True(result.IsSuccess);
            _breakRoomServiceMock.Verify(x => x.GetByRoomId(It.IsAny<Guid>()), Times.Once);
            _twilioParticipantRepositoryMock.Verify(x=>x.Update(It.IsAny<TwilioParticipant>()),Times.Once);
            _twilioParticipantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<TwilioParticipant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
        }
    }
}