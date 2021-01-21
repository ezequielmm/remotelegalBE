using System;
using System.Threading.Tasks;
using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class BreakRoomServiceTest
    {
        private readonly BreakRoomService _service;

        private readonly Mock<IBreakRoomRepository> _breakRoomRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;

        public BreakRoomServiceTest()
        {
            _breakRoomRepositoryMock = new Mock<IBreakRoomRepository>();
            _userServiceMock = new Mock<IUserService>();
            _roomServiceMock = new Mock<IRoomService>();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User {Id = Guid.NewGuid(),EmailAddress = "foo@foo.com"});
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>())).ReturnsAsync(Result.Ok("foo"));

            _service = new BreakRoomService(_breakRoomRepositoryMock.Object, _userServiceMock.Object, _roomServiceMock.Object);
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
            var result = await _service.JoinBreakRoom(breakRoom.Id);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldAddTheUserIntoTheBreakRoom()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Room = new Room { Id = Guid.NewGuid(), Status = RoomStatus.InProgress }
            };

            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id);

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
    }
}
