using FluentResults;
using MediaToolkit;
using MediaToolkit.Model;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class CompositionServiceTest : IDisposable
    {
        private readonly CompositionService _service;
        private readonly Mock<ICompositionRepository> _compositionRepositoryMock;
        private readonly Mock<ITwilioService> _twilioServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<ILogger<CompositionService>> _loggerMock;
        private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueue;
        private readonly Mock<ICompositionHelper> _compositionHelperMock;
        private readonly Mock<IMediaToolKitWrapper> _IMediaToolKitWrapperMock;

        public CompositionServiceTest()
        {
            _compositionRepositoryMock = new Mock<ICompositionRepository>();
            _twilioServiceMock = new Mock<ITwilioService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<CompositionService>>();
            _backgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
            _compositionHelperMock = new Mock<ICompositionHelper>();
            _IMediaToolKitWrapperMock = new Mock<IMediaToolKitWrapper>();

            _service = new CompositionService(_compositionRepositoryMock.Object, _twilioServiceMock.Object,
                _roomServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object, _backgroundTaskQueue.Object, _compositionHelperMock.Object, _IMediaToolKitWrapperMock.Object);
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task PostDepoCompositionCallback_ShouldFail_CompositionNotFound()
        {
            //Arrange
            _compositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync((Composition)null);

            //Act
            var result = await _service.PostDepoCompositionCallback(new PostDepositionEditionDto());

            //Assert
            Assert.True(result.IsFailed);
            Assert.IsType<ResourceNotFoundError>(result.Errors[0]);
        }

        [Fact]
        public async Task PostDepoCompositionCallback_ShouldOk()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                }
            };
            var postDepositionEdition = new PostDepositionEditionDto()
            {
                Video = $"{compositionId}.test",
                ConfigurationId = "foo"
            };
            _compositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(composition);
            _compositionRepositoryMock.Setup(x => x.Update(It.IsAny<Composition>()));
            _backgroundTaskQueue.Setup(x => x.QueueBackgroundWorkItem(It.IsAny<BackgroundTaskDto>()));

            //Act
            var result = await _service.PostDepoCompositionCallback(postDepositionEdition);

            //Assert
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.Update(It.IsAny<Composition>()), Times.Once);
            _backgroundTaskQueue.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<BackgroundTaskDto>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteTwilioCompositionAndRecordings_ShouldOk()
        {
            //Arrange
            _twilioServiceMock.Setup(x => x.DeleteCompositionAndRecordings(It.IsAny<DeleteTwilioRecordingsDto>())).ReturnsAsync(Result.Ok());

            //Act
            var result = await _service.DeleteTwilioCompositionAndRecordings(new DeleteTwilioRecordingsDto());

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task StoreCompositionMediaAsync_ShouldOk()
        {
            //Arrange      
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                }
            };
            _twilioServiceMock.Setup(x => x.GetCompositionMediaAsync(It.IsAny<Composition>())).ReturnsAsync(true);
            _twilioServiceMock.Setup(x => x.UploadCompositionMediaAsync(It.IsAny<Composition>()));
            _compositionRepositoryMock.Setup(x => x.Update(It.IsAny<Composition>()));
            _IMediaToolKitWrapperMock.Setup(x => x.GetVideoDuration(It.IsAny<string>())).Returns(It.IsAny<int>);

            //Act
            var result = await _service.StoreCompositionMediaAsync(composition);

            //Assert
            _twilioServiceMock.Verify(x => x.GetCompositionMediaAsync(It.IsAny<Composition>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.Update(It.IsAny<Composition>()), Times.Once);
            _twilioServiceMock.Verify(x => x.UploadCompositionMediaAsync(It.IsAny<Composition>()), Times.Once);
            _IMediaToolKitWrapperMock.Verify(x => x.GetVideoDuration(It.IsAny<string>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task StoreCompositionMediaAsync_ShouldFail()
        {
            //Arrange
            var expectedMessage = "Could not download composition media.";
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                }
            };
            _twilioServiceMock.Setup(x => x.GetCompositionMediaAsync(It.IsAny<Composition>())).ReturnsAsync(false);

            //Act
            var result = await _service.StoreCompositionMediaAsync(composition);

            //Assert
            _twilioServiceMock.Verify(x => x.GetCompositionMediaAsync(It.IsAny<Composition>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Equal(expectedMessage, result.Errors.First().Message);
        }

        [Fact]
        public async Task GetCompositionByRoom_ShouldOk()
        {
            //Arrange
            var roomSid = Guid.NewGuid();
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                }
            };
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(composition);

            //Act
            var result = await _service.GetCompositionByRoom(roomSid);

            //Assert
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value, composition);
        }

        [Fact]
        public async Task GetCompositionByRoom_ShouldFail()
        {
            //Arrange
            var roomSid = Guid.NewGuid();

            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Composition)null);

            //Act
            var result = await _service.GetCompositionByRoom(roomSid);

            //Assert
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.IsType<ResourceNotFoundError>(result.Errors.First());
        }

        [Fact]
        public async Task UpdateComposition_ShouldOk()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var currentTime = DateTime.UtcNow;
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
                LastUpdated = currentTime
            };
            _compositionRepositoryMock.Setup(x => x.Update(It.IsAny<Composition>())).ReturnsAsync(composition);

            //Act
            var result = await _service.UpdateComposition(composition);

            //Assert
            _compositionRepositoryMock.Verify(x => x.Update(It.IsAny<Composition>()), Times.Once);
            Assert.True(result.LastUpdated > currentTime);
        }

        [Fact]
        public async Task UpdateCompositionCallback_ShouldFail_CompositionBySidNotFound()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
            };
            _roomServiceMock
                .Setup(x => x.GetRoomBySId(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok((Room)null));
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Composition)null);

            //Act
            var result = await _service.UpdateCompositionCallback(composition);

            //Assert
            _roomServiceMock.Verify(x => x.GetRoomBySId(It.IsAny<string>()), Times.Once);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task UpdateCompositionCallback_ShouldFail_GetDepositionByRoomIdNotfound()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
            };
            _roomServiceMock
                .Setup(x => x.GetRoomBySId(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new Room()));
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Composition());
            _depositionServiceMock
                .Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail("Error"));

            //Act
            var result = await _service.UpdateCompositionCallback(composition);

            //Assert
            _roomServiceMock.Verify(x => x.GetRoomBySId(It.IsAny<string>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task UpdateCompositionCallback_ShouldFail_UploadCompositionMetadata()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
                Status = CompositionStatus.Available
            };
            var room = new Room()
            {
                Composition = composition,
                EndDate = DateTime.Now
            };
            var deposition = new Deposition()
            {
                Room = room,
                Events = new List<DepositionEvent>()
                {
                    new DepositionEvent() { EventType = EventType.OnTheRecord }
                },
                TimeZone = USTimeZone.AZ.ToString()
            };
            _roomServiceMock
                .Setup(x => x.GetRoomBySId(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new Room()));
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Composition());
            _depositionServiceMock
                .Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));
            _twilioServiceMock
                .Setup(x => x.GetVideoStartTimeStamp(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(DateTime.UtcNow));
            _twilioServiceMock
                .Setup(x => x.UploadCompositionMetadata(It.IsAny<CompositionRecordingMetadata>()))
                .ReturnsAsync(Result.Fail("Error"));

            //Act
            var result = await _service.UpdateCompositionCallback(composition);

            //Assert
            _roomServiceMock.Verify(x => x.GetRoomBySId(It.IsAny<string>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _depositionServiceMock.Verify(x => x.GetDepositionByRoomId(It.IsAny<Guid>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GetVideoStartTimeStamp(It.IsAny<string>()), Times.Once);
            _twilioServiceMock.Verify(x => x.UploadCompositionMetadata(It.IsAny<CompositionRecordingMetadata>()), Times.Once);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task UpdateCompositionCallback_ShouldFail_NullDepositionEndDate()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
                Status = CompositionStatus.Available
            };
            var room = new Room()
            {
                SId = "WrongRoomSid",
                Composition = composition,
                EndDate = null
            };
            var deposition = new Deposition()
            {
                Room = room,
                Events = new List<DepositionEvent>()
                {
                    new DepositionEvent() { EventType = EventType.OnTheRecord }
                },
                TimeZone = USTimeZone.AZ.ToString()
            };
            _roomServiceMock
                .Setup(x => x.GetRoomBySId(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new Room()));
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Composition());
            _depositionServiceMock
                .Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));
            _twilioServiceMock
                .Setup(x => x.GetVideoStartTimeStamp(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(DateTime.UtcNow));
            _twilioServiceMock
                .Setup(x => x.UploadCompositionMetadata(It.IsAny<CompositionRecordingMetadata>()))
                .ReturnsAsync(Result.Fail("Error"));
            var errorMessaggeExpected = string.Format("Error mapping Deposition->CompositionRecordingMetadata: EndDate property cannot be null - Deposition Room Sid \"{0}\"", room.SId);
            //Act
            var result = await _service.UpdateCompositionCallback(composition);

            //Assert
            _roomServiceMock.Verify(x => x.GetRoomBySId(It.IsAny<string>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _depositionServiceMock.Verify(x => x.GetDepositionByRoomId(It.IsAny<Guid>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GetVideoStartTimeStamp(It.IsAny<string>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessaggeExpected, result.Errors.First().Message);
        }

        [Fact]
        public async Task UpdateCompositionCallback_ShouldOk()
        {
            //Arrange
            var compositionId = Guid.NewGuid();
            var composition = new Composition()
            {
                Id = compositionId,
                SId = "CompositionTestSid",
                Room = new Room()
                {
                    SId = "RoomTestSid"
                },
                Status = CompositionStatus.Available
            };
            var room = new Room()
            {
                Composition = composition,
                EndDate = DateTime.Now
            };
            var deposition = new Deposition()
            {
                Room = room,
                Events = new List<DepositionEvent>()
                {
                    new DepositionEvent() { EventType = EventType.OnTheRecord }
                },
                TimeZone = USTimeZone.AZ.ToString()
            };
            _roomServiceMock
                .Setup(x => x.GetRoomBySId(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new Room()));
            _compositionRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Composition());
            _compositionRepositoryMock
                .Setup(x => x.Update(It.IsAny<Composition>()))
                .ReturnsAsync(composition);
            _depositionServiceMock
                .Setup(x => x.GetDepositionByRoomId(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));
            _twilioServiceMock
                .Setup(x => x.GetVideoStartTimeStamp(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(DateTime.UtcNow));
            _twilioServiceMock
                .Setup(x => x.UploadCompositionMetadata(It.IsAny<CompositionRecordingMetadata>()))
                .ReturnsAsync(Result.Ok());

            //Act
            var result = await _service.UpdateCompositionCallback(composition);

            //Assert
            _roomServiceMock.Verify(x => x.GetRoomBySId(It.IsAny<string>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _depositionServiceMock.Verify(x => x.GetDepositionByRoomId(It.IsAny<Guid>()), Times.Once);
            _twilioServiceMock.Verify(x => x.GetVideoStartTimeStamp(It.IsAny<string>()), Times.Once);
            _twilioServiceMock.Verify(x => x.UploadCompositionMetadata(It.IsAny<CompositionRecordingMetadata>()), Times.Once);
            _compositionRepositoryMock.Verify(x => x.Update(It.IsAny<Composition>()), Times.Once);
            Assert.True(result.IsSuccess);
        }
    }
}
