using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class RoomServiceTests : IDisposable
    {        
        private readonly List<Room> _rooms = new List<Room>();

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
            roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(_rooms.FirstOrDefault());           

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetById(roomId);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()), Times.Once());
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
            roomRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Room) null);

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.GetById(roomId);

            // Assert
            roomRepositoryMock.Verify(mock => mock.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<string[]>()), Times.Once());
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
                .ReturnsAsync((List<Room>) null);

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

            var roomService = InitializeService(twilioService: twilioServiceMock, roomRepository: roomRepositoryMock);

            // Act
            var result = await roomService.EndRoom(room);

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
            var result = await roomService.EndRoom(room);

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

            // Act
            var result = await roomService.StartRoom(room);

            // Assert            
            Assert.True(result.IsFailed);
        }

        private RoomService InitializeService(
            Mock<ITwilioService> twilioService = null,
            Mock<IRoomRepository> roomRepository = null)
        {
            var twilioServiceMock = twilioService ?? new Mock<ITwilioService>();
            var roomRepositoryMock = roomRepository ?? new Mock<IRoomRepository>();

            return new RoomService(
                twilioServiceMock.Object,
                roomRepositoryMock.Object
                );
        }
    }
}
