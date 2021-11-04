using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using Moq;
using Newtonsoft.Json;
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
        private readonly Mock<ISignalRDepositionManager> _signalRNotificationManagerMock;
        private readonly Mock<IMapper<BreakRoom, BreakRoomDto, object>> _breakRoomMapperMock;

        public BreakRoomServiceTest()
        {
            _breakRoomRepositoryMock = new Mock<IBreakRoomRepository>();
            _userServiceMock = new Mock<IUserService>();
            _roomServiceMock = new Mock<IRoomService>();
            _signalRNotificationManagerMock = new Mock<ISignalRDepositionManager>();
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
        public async Task JoinBreakRoom_ShouldFailBreakRoomRepositoryGetById()
        {
            // Arrange
            var invalidBreakRoomId = new Guid();
            BreakRoom breakRoom = null;
            _breakRoomRepositoryMock
                .Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(invalidBreakRoomId, null);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldFailGenerateRoomToken()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true
            };
            _roomServiceMock
                .Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>()))
                .ReturnsAsync(Result.Fail("Fail"));

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, null);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldFailBreakRoomLocked()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true,
                Name = "Test"
            };
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);
            var participant = Mock.Of<Participant>();
            participant.Role = ParticipantType.Attorney;
            var expectedError= $"The Break Room [{breakRoom.Name}] is currently locked.";
            
            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, participant);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(x => x == breakRoom.Id), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldAddTheUserIntoCreatedBreakRoom()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "foo@foo.com" };
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Room = new Room { Id = Guid.NewGuid(), Status = RoomStatus.Created }
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
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(), It.IsAny<RoomStatusEnum>())).ReturnsAsync(new List<RoomResource>());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok("foo"));
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, participant);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.Update(It.IsAny<BreakRoom>()), Times.Once);
            Assert.True(result.IsSuccess);
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
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok("foo"));
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, participant);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.Update(It.IsAny<BreakRoom>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldAddUserAndUpdateAttendees()
        {
            // Arrange
            var currentBreakRoomAtendeeUser = Mock.Of<User>();
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "foo@foo.com" };
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Room = new Room { Id = Guid.NewGuid(), Status = RoomStatus.InProgress },
                Attendees = new List<BreakRoomAttendee>() { 
                    new BreakRoomAttendee() { 
                        User = currentBreakRoomAtendeeUser,
                        UserId = currentBreakRoomAtendeeUser.Id
                    }
                }
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
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(), It.IsAny<RoomStatusEnum>())).ReturnsAsync(new List<RoomResource>());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok("foo"));
            _breakRoomRepositoryMock.Setup(x => x.Update(It.IsAny<BreakRoom>()));
            _breakRoomRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).
                ReturnsAsync(breakRoom);

            // Act
            var result = await _service.JoinBreakRoom(breakRoom.Id, participant);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Once);
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

        [Fact]
        public async Task GetByRoomId_ShouldReturnBreakRoom()
        {
            // Arrange
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true,
            };
            _breakRoomRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<BreakRoom, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(breakRoom);

            // Act
            var result = await _service.GetByRoomId(breakRoom.Id);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<BreakRoom, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(breakRoom.Id, result.Value.Id);
        }

        [Fact]
        public async Task GetByRoomId_ShouldFailInvalidId()
        {
            // Arrange
            Guid breakRoomId = new Guid();
            _breakRoomRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<BreakRoom, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((BreakRoom)null);
            var expectedError = $"BreakRoom not found with Room ID: {breakRoomId}";

            // Act
            var result = await _service.GetByRoomId(breakRoomId);

            // Assert
            _breakRoomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<BreakRoom, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveAttendeeCallback_ShouldOk()
        {
            // Arrange
            var identity = new TwilioIdentity() { 
                Email = "test@test.com",
                FirstName = "John Doe",
                Role = "Tester"
            };
            var userMock = Mock.Of<User>();
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = true,
                Attendees = new List<BreakRoomAttendee>() { 
                    new BreakRoomAttendee(){ 
                        User = userMock,
                    }
                }
            };
            
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(userMock));
            _signalRNotificationManagerMock
                .Setup(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()));

            // Act
            var result = await _service.RemoveAttendeeCallback(breakRoom, JsonConvert.SerializeObject(identity));

            // Assert
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Once);
            _userServiceMock.Verify(x => x.GetUserByEmail(It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RemoveAttendeeCallback_ShouldOkBreakRoomNotLocked()
        {
            // Arrange
            var identity = new TwilioIdentity()
            {
                Email = "test@test.com",
                FirstName = "John Doe",
                Role = "Tester"
            };
            var userMock = Mock.Of<User>();
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Attendees = new List<BreakRoomAttendee>() {
                    new BreakRoomAttendee(){
                        User = userMock,
                    }
                }
            };

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(userMock));
            _signalRNotificationManagerMock
                .Setup(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()));

            // Act
            var result = await _service.RemoveAttendeeCallback(breakRoom, JsonConvert.SerializeObject(identity));

            // Assert
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Never);
            _userServiceMock.Verify(x => x.GetUserByEmail(It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RemoveAttendeeCallback_ShouldFailGettingUser()
        {
            // Arrange
            var identity = new TwilioIdentity()
            {
                Email = "test@test.com",
                FirstName = "John Doe",
                Role = "Tester"
            };
            var userMock = Mock.Of<User>();
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Attendees = new List<BreakRoomAttendee>() {
                    new BreakRoomAttendee(){
                        User = userMock,
                    }
                }
            };

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail("Error"));
            var expectedError = $"User not found with email: {identity.Email}";
            // Act
            var result = await _service.RemoveAttendeeCallback(breakRoom, JsonConvert.SerializeObject(identity));

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveAttendeeCallback_ShouldFailAttendeeNotFoundWithUserId()
        {
            // Arrange
            var identity = new TwilioIdentity()
            {
                Email = "test@test.com",
                FirstName = "John Doe",
                Role = "Tester"
            };
            var userMock = Mock.Of<User>();
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false
            };

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(userMock));
            var expectedError = $"Attendee not found with User Id: {userMock.Id}";
            // Act
            var result = await _service.RemoveAttendeeCallback(breakRoom, JsonConvert.SerializeObject(identity));

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }
    }
}
