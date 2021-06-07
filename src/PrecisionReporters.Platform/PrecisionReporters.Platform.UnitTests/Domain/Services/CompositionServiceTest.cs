using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        public CompositionServiceTest()
        {
            _compositionRepositoryMock = new Mock<ICompositionRepository>();
            _twilioServiceMock = new Mock<ITwilioService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<CompositionService>>();
            _backgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
            _compositionHelperMock = new Mock<ICompositionHelper>();

            _service = new CompositionService(_compositionRepositoryMock.Object, _twilioServiceMock.Object,
                _roomServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object, _backgroundTaskQueue.Object, _compositionHelperMock.Object);
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
    }
}
