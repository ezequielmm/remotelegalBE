using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class NotificationsControllerTest
    {
        private readonly Mock<ISnsNotificationService> _snsNotificationService;
        public NotificationsControllerTest()
        {
            _snsNotificationService = new Mock<ISnsNotificationService>();
        }

        [Fact]
        public async Task ExhibitProcessCallback_ReturnsOk()
        {
            // Arrange
            var data = "Hello World!!";
            var controllerContext = ContextFactory.GetControllerContext(data);
            var controller = new NotificationsController(_snsNotificationService.Object)
            {
                ControllerContext = controllerContext
            };
            _snsNotificationService
                .Setup(mock => mock.Notify(It.IsAny<Stream>()))
                .ReturnsAsync(Result.Ok(string.Empty));
            // Act
            var result = await controller.SnsCallback();
            // Assert
            _snsNotificationService.Verify(mock => mock.Notify(It.IsAny<Stream>()), Times.Once);
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
        }

        [Fact]
        public async Task ExhibitProcessCallback_ReturnsFail()
        {
            // Arrange
            var data = "Fail Message";
            var controllerContext = ContextFactory.GetControllerContext(data);
            var controller = new NotificationsController(_snsNotificationService.Object)
            {
                ControllerContext = controllerContext
            };
            _snsNotificationService
                .Setup(mock => mock.Notify(It.IsAny<Stream>()))
                .ReturnsAsync(Result.Fail(string.Empty));
            // Act
            var result = await controller.SnsCallback();
            // Assert
            _snsNotificationService.Verify(mock => mock.Notify(It.IsAny<Stream>()), Times.Once);
            var statusResult = Assert.IsType<StatusCodeResult>(result);
            Assert.NotNull(statusResult);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);
        }
    }
}