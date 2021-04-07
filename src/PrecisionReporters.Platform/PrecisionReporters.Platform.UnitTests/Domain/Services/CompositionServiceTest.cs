using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System.Linq.Expressions;
using PrecisionReporters.Platform.Domain.Errors;
using FluentResults;

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

        public CompositionServiceTest()
        {
            _compositionRepositoryMock = new Mock<ICompositionRepository>();
            _twilioServiceMock = new Mock<ITwilioService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<CompositionService>>();
            _backgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
            //_compositionMapperMock = new Mock<IMapper<Composition, CompositionDto, object>>();

            _service = new CompositionService(_compositionRepositoryMock.Object, _twilioServiceMock.Object,
                _roomServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object, _backgroundTaskQueue.Object);
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task GetDepositionRecordingIntervals()
        {
            var events = new List<DepositionEvent>();
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(1), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(25), EventType = EventType.OffTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(56), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(125), EventType = EventType.OffTheRecord });
            var result = await Task.Run(() => _service.GetDepositionRecordingIntervals(events, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()));

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task PostDepoCompositionCallback_ShouldFail_CompositionNotFound()
        {
            //Arrange
            _compositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync((Composition)null);

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
            _compositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(composition);
            _compositionRepositoryMock.Setup(x => x.Update(It.IsAny<Composition>()));
            _backgroundTaskQueue.Setup(x => x.QueueBackgroundWorkItem(It.IsAny<BackgroundTaskDto>()));

            //Act
            var result = await _service.PostDepoCompositionCallback(postDepositionEdition);

            //Assert
            _compositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Composition, bool>>>(), It.IsAny<string[]>()), Times.Once);
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
    }
}
