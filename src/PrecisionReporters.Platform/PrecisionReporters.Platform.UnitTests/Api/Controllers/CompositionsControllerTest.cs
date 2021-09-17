using Amazon.SimpleNotificationService.Util;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class CompositionsControllerTest
    {
        private readonly Mock<ICompositionService> _compositionService;
        private readonly Mock<ILogger<CompositionsController>> _logger;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;
        private readonly Mock<ITwilioCallbackService> _twilioCallbackService;
        private readonly Mock<ISnsHelper> _snsHelper;
        private readonly Mock<IAwsSnsWrapper> _awsSnsWrapper;
        private readonly CompositionsController _classUnderTest;

        public CompositionsControllerTest()
        {
            _compositionService = new Mock<ICompositionService>();
            _compositionMapper = new CompositionMapper();
            _logger = new Mock<ILogger<CompositionsController>>();
            _twilioCallbackService = new Mock<ITwilioCallbackService>();
            _snsHelper = new Mock<ISnsHelper>();
            _awsSnsWrapper = new Mock<IAwsSnsWrapper>();
            _classUnderTest = new CompositionsController(_compositionService.Object,
                _compositionMapper,
                _logger.Object,
                _twilioCallbackService.Object,
                _snsHelper.Object,
                _awsSnsWrapper.Object);
        }

        [Fact]
        public async Task CompositionStatusCallback_ReturnOk()
        {
            // Arrange
            var compositionDto = new CallbackCompositionDto
            {
                StatusCallbackEvent = "Available-mockStatus",
                CompositionSid = Guid.NewGuid().ToString(),
                Url = "mockUrl",
                MediaUri = "mediaUri",
                RoomSid = Guid.NewGuid().ToString()
            };
            _compositionService
                .Setup(mock => mock.UpdateCompositionCallback(It.IsAny<Composition>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.CompositionStatusCallback(compositionDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _compositionService.Verify(mock => mock.UpdateCompositionCallback(It.IsAny<Composition>()), Times.Once);
        }

        [Fact]
        public async Task CompositionStatusCallback_ReturnError_WhenUpdateCompositionCallbackFails()
        {
            // Arrange
            var compositionDto = new CallbackCompositionDto
            {
                StatusCallbackEvent = "Available-mockStatus",
                CompositionSid = Guid.NewGuid().ToString(),
                Url = "mockUrl",
                MediaUri = "mediaUri",
                RoomSid = Guid.NewGuid().ToString()
            };
            _compositionService
                .Setup(mock => mock.UpdateCompositionCallback(It.IsAny<Composition>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.CompositionStatusCallback(compositionDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _compositionService.Verify(mock => mock.UpdateCompositionCallback(It.IsAny<Composition>()), Times.Once);
        }

        [Fact]
        public async Task RoomStatusCallback_ReturnOk()
        {
            // Arrange
            _twilioCallbackService
                .Setup(mock => mock.UpdateStatusCallback(It.IsAny<RoomCallbackDto>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.RoomStatusCallback(It.IsAny<RoomCallbackDto>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _twilioCallbackService.Verify(mock => mock.UpdateStatusCallback(It.IsAny<RoomCallbackDto>()), Times.Once);
        }

        [Fact]
        public async Task RoomStatusCallback_ReturnError_WhenUpdateStatusCallbackFails()
        {
            // Arrange
            _twilioCallbackService
                .Setup(mock => mock.UpdateStatusCallback(It.IsAny<RoomCallbackDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.RoomStatusCallback(It.IsAny<RoomCallbackDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _twilioCallbackService.Verify(mock => mock.UpdateStatusCallback(It.IsAny<RoomCallbackDto>()), Times.Once);
        }

        [Fact]
        public async Task CompositionEditionCallback_ReturnOk_WhenIsNotificationType()
        {
            // Arrange  
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _classUnderTest.ControllerContext = context;
            _awsSnsWrapper
                .Setup(mock => mock.ParseMessage(It.IsAny<string>()))
                .Returns(Message.ParseMessage(ContextFactory.GetContextRequestBodyWithNotificationType()));
            _awsSnsWrapper
                .Setup(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()))
                .Returns(true);

            // Act
            var result = await _classUnderTest.CompositionEditionCallback();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _awsSnsWrapper.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _awsSnsWrapper.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _snsHelper.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Never);
            _compositionService.Verify(mock=>mock.PostDepoCompositionCallback(It.IsAny<PostDepositionEditionDto>()),Times.Once);
        }

        [Fact]
        public async Task CompositionEditionCallback_ReturnOk_WhenIsSubscriptionType()
        {
            // Arrange  
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _classUnderTest.ControllerContext = context;
            _awsSnsWrapper
                .Setup(mock => mock.ParseMessage(It.IsAny<string>()))
                .Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _awsSnsWrapper
                .Setup(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()))
                .Returns(true);
            _snsHelper
                .Setup(mock => mock.SubscribeEndpoint(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.CompositionEditionCallback();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _awsSnsWrapper.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _awsSnsWrapper.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _snsHelper.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _compositionService.Verify(mock=>mock.PostDepoCompositionCallback(It.IsAny<PostDepositionEditionDto>()),Times.Never);
        }

        [Fact]
        public async Task CompositionEditionCallback_ReturnBadRequest_WhenIsMessageSignatureValidFails()
        {
            // Arrange  
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _classUnderTest.ControllerContext = context;
            _awsSnsWrapper
                .Setup(mock => mock.ParseMessage(It.IsAny<string>()))
                .Returns(It.IsAny<Message>());
            _awsSnsWrapper
                .Setup(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()))
                .Returns(false);
            
            // Act
            var result = await _classUnderTest.CompositionEditionCallback();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestResult>(result);
            _awsSnsWrapper.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _awsSnsWrapper.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _snsHelper.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Never);
            _compositionService.Verify(mock=>mock.PostDepoCompositionCallback(It.IsAny<PostDepositionEditionDto>()),Times.Never);
        }

        [Fact]
        public async Task CompositionEditionCallback_ReturnBadRequest_WhenExceptionHappendInJsonConvert()
        {
            // Arrange  
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _classUnderTest.ControllerContext = context;
            _awsSnsWrapper
                .Setup(mock => mock.ParseMessage(It.IsAny<string>()))
                .Returns(Message.ParseMessage(ContextFactory.GetContextRequestBodyWithNotificationTypeForException()));
            _awsSnsWrapper
                .Setup(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()))
                .Returns(true);

            // Act
            var result = await _classUnderTest.CompositionEditionCallback();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
            _awsSnsWrapper.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _awsSnsWrapper.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _snsHelper.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Never);
            _compositionService.Verify(mock=>mock.PostDepoCompositionCallback(It.IsAny<PostDepositionEditionDto>()),Times.Never);
        }
    }
}