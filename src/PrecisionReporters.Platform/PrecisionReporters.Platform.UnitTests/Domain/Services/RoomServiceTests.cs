using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Dtos;
using Twilio.Exceptions;
using Twilio.Rest.Video.V1;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class RoomServiceTests : IDisposable
    {
        private readonly List<Room> _rooms = new List<Room>();


        private readonly Mock<ITwilioService> _twilioServiceMock;
        private readonly Mock<IRoomRepository> _roomRepositoryMock;
        private readonly RoomService _service;
        private readonly Mock<IUserRepository> _userRepositoryMock = new Mock<IUserRepository>();
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock = new Mock<IDepositionRepository>();
        private readonly Mock<ILogger<RoomService>> _mockLogger = new Mock<ILogger<RoomService>>();

        public RoomServiceTests()
        {
            _twilioServiceMock = new Mock<ITwilioService>();
            _roomRepositoryMock = new Mock<IRoomRepository>();

            _service = new RoomService(_twilioServiceMock.Object, _roomRepositoryMock.Object, _userRepositoryMock.Object, _depositionRepositoryMock.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task GetRoomById_ShouldReturn_RoomWithGivenId()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);

            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();

            var roomRepositoryMock = new Mock<IRoomRepository>();
            roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(_rooms.FirstOrDefault());

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetById(roomId);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRoomById_ShouldReturnFail_WhenRoomDoesNotExist()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);

            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();

            var roomRepositoryMock = new Mock<IRoomRepository>();
            roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Room)null);

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetById(roomId);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once());
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetRoomByName_ShouldReturn_RoomWithGivenName()
        {
            // Arrange
            var roomName = "RoomTest";
            var room = RoomFactory.GetRoomByName(roomName);

            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();

            var roomRepositoryMock = new Mock<IRoomRepository>();
            roomRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(_rooms);

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetByName(roomName);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRoomByName_ShouldReturnFail_WhenRoomDoesNotExist()
        {
            // Arrange
            var roomName = "RoomTest";
            var room = RoomFactory.GetRoomByName(roomName);

            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();

            var roomRepositoryMock = new Mock<IRoomRepository>();
            roomRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((List<Room>)null);

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetByName(roomName);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task EndRoom_ShouldReturnRoomStatusCompleted_WhenRoomIsEnded()
        {
            // Arrange
            var room = RoomFactory.GetRoomWithInProgressStatus();
            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();
            var roomRepositoryMock = new Mock<IRoomRepository>();
            twilioServiceMock.Setup(x => x.EndRoom(It.IsAny<Room>())).ReturnsAsync(Result.Ok());
            twilioServiceMock.Setup(x => x.CreateComposition(It.IsAny<Room>(), It.IsAny<string>())).ReturnsAsync((CompositionResource)null);
            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.EndRoom(room, "witness@mail.com");

            // Assert            
            Assert.True(result.Value.Status == RoomStatus.Completed);
        }

        [Fact]
        public async Task EndRoom_ShouldReturnFail_WhenRoomStatusIsNotInProgress()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            var errorMessage = $"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}";
            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();
            var roomRepositoryMock = new Mock<IRoomRepository>();

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.EndRoom(room, "witness@mail.com");

            // Assert            
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task StartRoom_ShouldReturnError_WhenRoomStatusIsNotEqualToCreated()
        {
            // Arrange
            var room = RoomFactory.GetRoomWithInProgressStatus();
            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();
            var roomRepositoryMock = new Mock<IRoomRepository>();

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            twilioServiceMock.Setup(x => x.CreateRoom(It.IsAny<Room>(), It.IsAny<bool>())).ReturnsAsync(room);

            // Act
            var result = await roomService.StartRoom(room, true);

            // Assert            
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task StartRoom_ShouldReturnRoom_WhenRoomIsAlreadyStarted()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();
            twilioServiceMock.Setup(x => x.CreateRoom(It.IsAny<Room>(), It.IsAny<bool>())).Throws(new ApiException(ApplicationConstants.RoomExistError));
            var roomRepositoryMock = new Mock<IRoomRepository>();

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.StartRoom(room, true);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task StartRoom_ShouldReturnRoom_WhenRoomStatusIsEqualToCreated()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            _rooms.Add(room);

            var twilioServiceMock = new Mock<ITwilioService>();
            twilioServiceMock.Setup(x => x.CreateRoom(It.IsAny<Room>(), It.IsAny<bool>())).ReturnsAsync(new Room());
            var roomRepositoryMock = new Mock<IRoomRepository>();

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.StartRoom(room, true);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateRoomToken_ShouldReturnFail_IfRoomNotFound()
        {
            // Arrange
            var roomName = "TestingRoom";
            var expectedError = $"Room {roomName} not found";
            _roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync((Room)null);

            // Act
            var result = await _service.GenerateRoomToken(roomName, new User(), ParticipantType.Attorney, "any@mail.com");

            // Assert
            _roomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(x => x.Message));
        }
        [Theory]
        [InlineData(RoomStatus.Completed)]
        [InlineData(RoomStatus.Created)]
        [InlineData(RoomStatus.Failed)]
        public async Task GenerateRoomToken_ShouldReturnFail_IfRoomNotInProgress(RoomStatus roomStatus)
        {
            // Arrange
            var roomName = "TestingRoom";
            var room = new Room { Name = roomName, Status = roomStatus };
            var expectedError = $"There was an error ending the the Room '{roomName}'. It's not in progress. Current state: {roomStatus}";
            _roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(room);

            // Act
            var result = await _service.GenerateRoomToken(roomName, new User(), ParticipantType.Attorney, "any@mail.com");

            // Assert
            _roomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(x => x.Message));
        }

        [Fact]
        public async Task GenerateRoomToken_ShouldReturn_TwilioToken()
        {
            // Arrange
            var roomName = "TestingRoom";
            var room = new Room { Name = roomName, Status = RoomStatus.InProgress };
            var participantRole = ParticipantType.Observer;
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "testUser@mail.com", FirstName = "userFirstName", LastName = "userLastName" };
            var identityObject = new TwilioIdentity
            {
                Name = $"{user.FirstName} {user.LastName}",
                Role = Enum.GetName(typeof(ParticipantType), participantRole),
                Email = user.EmailAddress
            };

            var token = "TestingToken";
            _roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(room);
            _twilioServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<TwilioIdentity>(), It.IsAny<bool>())).Returns(token);

            // Act
            var result = await _service.GenerateRoomToken(roomName, user, participantRole, user.EmailAddress);

            // Assert
            _roomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GenerateToken(It.Is<string>(a => a == roomName), It.Is<TwilioIdentity>(a => a.Email == identityObject.Email), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(token, result.Value);
        }

        [Fact]
        public async Task GenerateRoomToken_ShouldReturn_IfChatRoomIsNotNull()
        {
            // Arrange
            var roomName = "TestingRoom";
            var room = new Room { Name = roomName, Status = RoomStatus.InProgress };
            var participantRole = ParticipantType.Observer;
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "testUser@mail.com", FirstName = "userFirstName", LastName = "userLastName" };
            var identityObject = new TwilioIdentity
            {
                Name = $"{user.FirstName} {user.LastName}",
                Role = Enum.GetName(typeof(ParticipantType), participantRole),
                Email = user.EmailAddress
            };

            var token = "TestingToken";
            var chatDto = new ChatDto()
            {
                AddParticipant = true,
                ChatName = "MockChat",
                CreateChat = true,
                SId = Guid.NewGuid().ToString()
            };
            _roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(room);
            _twilioServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<TwilioIdentity>(), It.IsAny<bool>())).Returns(token);
            _twilioServiceMock
                .Setup(mock => mock.CreateChatUser(It.IsAny<TwilioIdentity>()))
                .ReturnsAsync(Result.Ok("userChatSiD"));

            // Act
            var result = await _service.GenerateRoomToken(roomName, user, participantRole, user.EmailAddress,chatDto);

            // Assert
            _roomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GenerateToken(It.Is<string>(a => a == roomName), It.Is<TwilioIdentity>(a => a.Email == identityObject.Email), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(token, result.Value);
        }

        [Fact]
        public async Task GenerateRoomToken_ShouldReturn_IfChatRoomIsNotNullAndEmptySId()
        {
            // Arrange
            var roomName = "TestingRoom";
            var room = new Room { Name = roomName, Status = RoomStatus.InProgress };
            var participantRole = ParticipantType.Observer;
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "testUser@mail.com", FirstName = "userFirstName", LastName = "userLastName", SId = Guid.NewGuid().ToString()};
            var identityObject = new TwilioIdentity
            {
                Name = $"{user.FirstName} {user.LastName}",
                Role = Enum.GetName(typeof(ParticipantType), participantRole),
                Email = user.EmailAddress
            };
            var token = "TestingToken";
            var chatDto = new ChatDto()
            {
                AddParticipant = true,
                ChatName = Guid.NewGuid().ToString(),
                CreateChat = true
            };
            _roomRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(room);
            _twilioServiceMock
                .Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<TwilioIdentity>(), It.IsAny<bool>()))
                .Returns(token);
            _twilioServiceMock
                .Setup(mock => mock.CreateChat(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok("userChatSiD"));
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(),null))
                .ReturnsAsync(DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid()));

            // Act
            var result = await _service.GenerateRoomToken(roomName, user, participantRole, user.EmailAddress,chatDto);

            // Assert
            _roomRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GenerateToken(It.Is<string>(a => a == roomName), It.Is<TwilioIdentity>(a => a.Email == identityObject.Email), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(token, result.Value);
        }

        [Fact]
        public async Task Create_ShouldReturn_Room()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
           _roomRepositoryMock
                .Setup(mock => mock.Create(It.IsAny<Room>()))
                .ReturnsAsync(room);
            
            // Act
            var result = await _service.Create(room);

            // Assert
            Assert.NotNull(result);
            _roomRepositoryMock.Verify(x => x.Create(It.IsAny<Room>()), Times.Once);
        }

        [Fact]
        public async Task GetRoomBySId_ShouldReturn_Room()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            _roomRepositoryMock
                .Setup(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(),null,It.IsAny<bool>()))
                .ReturnsAsync(room);
            
            // Act
            var result = await _service.GetRoomBySId(roomId.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Room>(result.Value);
            _roomRepositoryMock.Verify(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(),null,It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturn_UpdatedRoom()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = RoomFactory.GetRoomById(roomId);
            var updatedRoom = RoomFactory.GetRoomById(roomId);
            updatedRoom.Name = "Another Room Name";
            _roomRepositoryMock
                .Setup(mock => mock.Update(It.IsAny<Room>()))
                .ReturnsAsync(updatedRoom);
            
            // Act
            var result = await _service.Update(room);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Room>(result.Value);
            Assert.NotEqual(room.Name, result.Value.Name);
            _roomRepositoryMock.Verify(mock => mock.Update(It.IsAny<Room>()), Times.Once);
        }

        [Fact]
        public async Task GetTwilioRoomByNameAndStatus_ShouldReturn_RoomResource()
        {
            // Arrange
            var uniqueName = Guid.NewGuid().ToString();
            var status = RoomResource.RoomStatusEnum.InProgress;
            _twilioServiceMock
                .Setup(mock => mock.GetRoomsByUniqueNameAndStatus(It.IsAny<string>(), It.IsAny<RoomResource.RoomStatusEnum>()))
                .ReturnsAsync(new List<RoomResource>());

            // Act
            var result = await _service.GetTwilioRoomByNameAndStatus(uniqueName,status);

            // Assert
            Assert.NotNull(result);
            _twilioServiceMock.Verify(mock => mock.GetRoomsByUniqueNameAndStatus(It.IsAny<string>(), It.IsAny<RoomResource.RoomStatusEnum>()));
        }

        [Fact]
        public async Task RemoveRecordingRules_ShouldReturn_Bool()
        {
            // Arrange
            var roomSiD = Guid.NewGuid().ToString();
            _twilioServiceMock
                .Setup(mock => mock.RemoveRecordingRules(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.RemoveRecordingRules(roomSiD);

            // Assert
            Assert.True(result);
            _twilioServiceMock.Verify(mock => mock.RemoveRecordingRules(It.IsAny<string>()),Times.Once);
        }

        [Fact]
        public async Task AddRecordingRules_ShouldReturn_Bool()
        {
            // Arrange
            _twilioServiceMock
                .Setup(mock => mock.AddRecordingRules(It.IsAny<string>(),It.IsAny<TwilioIdentity>(),It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.AddRecordingRules(It.IsAny<string>(),It.IsAny<TwilioIdentity>(),It.IsAny<bool>());

            // Assert
            Assert.True(result);
            _twilioServiceMock.Verify(mock => mock.AddRecordingRules(It.IsAny<string>(),It.IsAny<TwilioIdentity>(),It.IsAny<bool>()), Times.Once);
        }

        private RoomService InitializeService(
            Mock<ITwilioService> twilioService = null,
            Mock<IRoomRepository> roomRepository = null,
            Mock<IUserRepository> userRepository = null,
            Mock<IDepositionRepository> depositionRepository = null)
        {
            var twilioServiceMock = twilioService ?? new Mock<ITwilioService>();
            var roomRepositoryMock = roomRepository ?? new Mock<IRoomRepository>();
            var userRepositoryMock = userRepository ?? new Mock<IUserRepository>();
            var depositionRepositoryMock = depositionRepository ?? new Mock<IDepositionRepository>();

            return new RoomService(
                twilioServiceMock.Object,
                roomRepositoryMock.Object,
                userRepositoryMock.Object,
                depositionRepositoryMock.Object,
                _mockLogger.Object
                );
        }
    }
}
