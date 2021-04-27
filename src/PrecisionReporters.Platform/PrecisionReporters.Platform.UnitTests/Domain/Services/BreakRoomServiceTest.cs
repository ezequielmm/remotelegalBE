using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Twilio.Rest.Video.V1;
using Xunit;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class BreakRoomServiceTest
    {
        private readonly BreakRoomService _service;

        private readonly Mock<IBreakRoomRepository> _breakRoomRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<ISignalRNotificationManager> _signalRNotificationManagerMock;
        private readonly Mock<IMapper<BreakRoom, BreakRoomDto, object>> _breakRoomMapperMock;

        public BreakRoomServiceTest()
        {
            _breakRoomRepositoryMock = new Mock<IBreakRoomRepository>();
            _userServiceMock = new Mock<IUserService>();
            _roomServiceMock = new Mock<IRoomService>();
            _signalRNotificationManagerMock = new Mock<ISignalRNotificationManager>();
            _breakRoomMapperMock = new Mock<IMapper<BreakRoom, BreakRoomDto, object>>();
            _service = new BreakRoomService(_breakRoomRepositoryMock.Object,
                _userServiceMock.Object,
                _roomServiceMock.Object,
                _breakRoomMapperMock.Object,
                _signalRNotificationManagerMock.Object);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldFailIfRoomIsLocked()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true
            };
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, null);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldAddTheUserIntoTheBreakRoom()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "foo@foo.com" };
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Room = new Room { Id = Guid.NewGuid(), Status = RoomStatus.InProgress }
            };
            var participant = new Participant() { User = user };
            var deposition = new Deposition()
            {
                Participants = new List<Participant>()
                {
                    participant
                }
            };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(),It.IsAny<RoomStatusEnum>())).ReturnsAsync(new List<RoomResource>());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok("foo"));
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, participant);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.Update(It.IsAny<BreakRoom>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task LockBreakRoom_ShouldFailIfNewStatusIsTheCurrentOne()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true
            };
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.LockBreakRoom(breakRoom.Id, breakRoom.IsLocked);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task LockBreakRoom_ShouldUpdateLockValue()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true
            };
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.LockBreakRoom(breakRoom.Id, !breakRoom.IsLocked);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.Update(It.IsAny<BreakRoom>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task LockBreakRoom_ShouldFail_BreakRoomNotFound()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true
            };
            var expectedError = $"Break Room with Id {breakRoom.Id} could not be found";
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync((BreakRoom)null);

            // Act
            var result = await _service.LockBreakRoom(breakRoom.Id, !breakRoom.IsLocked);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(x => x == breakRoom.Id), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }
    }
}
