using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class SystemSettingsControllerTests
    {
        private readonly Mock<ISystemSettingsService> _service;
        private readonly SystemSettingsController _controller;

        public SystemSettingsControllerTests()
        {
            _service = new Mock<ISystemSettingsService>();
            _controller = new SystemSettingsController(_service.Object);
        }

        [Fact]
        public async Task GetAll_Should_Fail()
        {
            //Arrange
            _service.Setup(x => x.GetAll()).ReturnsAsync(Result.Fail(new Error("Mock Message")));

            //Act
            var result = await _controller.SystemSettings();

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _service.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public async Task GetAll_Should_Ok()
        {
            //Arrange
            _service.Setup(x => x.GetAll()).ReturnsAsync(Result.Ok());

            //Act
            var controllerResult = await _controller.SystemSettings();

            //Assert
            Assert.NotNull(controllerResult);
            var result = Assert.IsType<OkObjectResult>(controllerResult);
            Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
            _service.Verify(x => x.GetAll(), Times.Once);
        }
    }
}
