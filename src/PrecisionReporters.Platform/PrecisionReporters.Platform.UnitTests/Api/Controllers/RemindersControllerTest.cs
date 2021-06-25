using Amazon.SimpleNotificationService.Util;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class RemindersControllerTest
    {
        private readonly Mock<IReminderService> _remindersServiceMock;
        private readonly Mock<ILogger<RemindersController>> _loggerMock;
        private readonly RemindersController _remindersController;
        private readonly Mock<IAwsSnsWrapper> _awsSnsWrapperMock;
        private readonly Mock<ISnsHelper> _snsHelperMock;

        public RemindersControllerTest()
        {
            _remindersServiceMock = new Mock<IReminderService>();
            _loggerMock = new Mock<ILogger<RemindersController>>();
            _awsSnsWrapperMock = new Mock<IAwsSnsWrapper>();
            _snsHelperMock = new Mock<ISnsHelper>();
            _remindersController = new RemindersController(_loggerMock.Object, _remindersServiceMock.Object, _awsSnsWrapperMock.Object, _snsHelperMock.Object);
        }

        [Fact]
        public async Task Reminder_ShouldFail_InvalidSignature()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(false);

            //Act
            var result = await _remindersController.Reminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<BadRequestResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
        }

        [Fact]
        public async Task Reminder_ShouldOk_SubscribeEndpointFail()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await _remindersController.Reminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _remindersServiceMock.Verify(s => s.SendReminder(), Times.Once);
        }

        [Fact]
        public async Task Reminder_ShouldOk_ReminderServiceThrowException()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _remindersServiceMock.Setup(s => s.SendReminder()).Throws(new Exception());

            //Act
            var result = await _remindersController.Reminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<BadRequestObjectResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _remindersServiceMock.Verify(s => s.SendReminder(), Times.Once);
        }

        [Fact]
        public async Task Reminder_ShouldOk()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;            
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Ok());

            //Act
            var result = await _remindersController.Reminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once); 
            _remindersServiceMock.Verify(s => s.SendReminder(), Times.Once);
        }

        [Fact]
        public async Task DailyReminder_ShouldFail_InvalidSignature()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(false);

            //Act
            var result = await _remindersController.DayBeforeReminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<BadRequestResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
        }

        [Fact]
        public async Task DailyReminder_ShouldOk_SubscribeEndpointFail()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await _remindersController.DayBeforeReminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _remindersServiceMock.Verify(s => s.SendDailyReminder(), Times.Once);
        }

        [Fact]
        public async Task DailyReminder_ShouldOk_ReminderServiceThrowException()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _remindersServiceMock.Setup(s => s.SendDailyReminder()).Throws(new Exception());

            //Act
            var result = await _remindersController.DayBeforeReminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<BadRequestObjectResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _remindersServiceMock.Verify(s => s.SendDailyReminder(), Times.Once);
        }

        [Fact]
        public async Task DailyReminder_ShouldOk()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithSnsRequestBody();
            _remindersController.ControllerContext = context;
            _awsSnsWrapperMock.Setup(m => m.IsMessageSignatureValid(It.IsAny<Message>())).Returns(true);
            _awsSnsWrapperMock.Setup(m => m.ParseMessage(It.IsAny<string>())).Returns(Message.ParseMessage(ContextFactory.GetContextRequestBody()));
            _snsHelperMock.Setup(h => h.SubscribeEndpoint(It.IsAny<string>())).ReturnsAsync(Result.Ok());

            //Act
            var result = await _remindersController.DayBeforeReminder();

            //Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkResult>(result);
            _awsSnsWrapperMock.Verify(mock => mock.IsMessageSignatureValid(It.IsAny<Message>()), Times.Once);
            _awsSnsWrapperMock.Verify(mock => mock.ParseMessage(It.IsAny<string>()), Times.Once);
            _snsHelperMock.Verify(mock => mock.SubscribeEndpoint(It.IsAny<string>()), Times.Once);
            _remindersServiceMock.Verify(s => s.SendDailyReminder(), Times.Once);
        }
    }
}
