using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class ChatControllerTest
    {
        private readonly Mock<IChatService> _chatServiceMock;
        private readonly ChatController controllerToTest;
        public ChatControllerTest()
        {
            _chatServiceMock = new Mock<IChatService>();
            controllerToTest = new ChatController(_chatServiceMock.Object);
        }

        [Fact]
        public async Task GetChatTokenById_Should_Fail()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            controllerToTest.ControllerContext = context;
            _chatServiceMock.Setup(x=>x.ManageChatParticipant(It.IsAny<Guid>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await controllerToTest.GetChatTokenById(Guid.NewGuid());

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _chatServiceMock.Verify(mock => mock.ManageChatParticipant(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetChatTokenById_Should_Ok()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            controllerToTest.ControllerContext = context;
            _chatServiceMock.Setup(x => x.ManageChatParticipant(It.IsAny<Guid>())).ReturnsAsync(Result.Ok());

            //Act
            var result = await controllerToTest.GetChatTokenById(Guid.NewGuid());

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _chatServiceMock.Verify(mock => mock.ManageChatParticipant(It.IsAny<Guid>()), Times.Once);
        }
    }
}
