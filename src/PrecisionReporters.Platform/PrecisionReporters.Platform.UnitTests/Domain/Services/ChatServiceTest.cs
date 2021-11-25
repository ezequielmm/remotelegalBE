using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class ChatServiceTest
    {
        private readonly Mock<ITwilioService> _twilioServiceMock;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly ChatService serviceToTest;

        public ChatServiceTest()
        {
            _twilioServiceMock = new Mock<ITwilioService>();
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<ChatService>>();
            serviceToTest = new ChatService(_twilioServiceMock.Object, _depositionRepositoryMock.Object, _userRepositoryMock.Object, _userServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ManageChatParticipant_ShouldFail_UserNotFound()
        {
            //Arrange
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((User)null);
            var expectedMessage = "User not found";

            //Act
            var result = await serviceToTest.ManageChatParticipant(Guid.NewGuid());

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedMessage, result.Errors.Select(x => x.Message));
        }

        [Fact]
        public async Task ManageChatParticipant_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(x => x.Id == depositionId, It.IsAny<string[]>(),
                It.IsAny<bool>()))
                .ReturnsAsync((Deposition)null);
            var expectedMessage = $"Deposition not found with ID: {depositionId}";

            //Act
            var result = await serviceToTest.ManageChatParticipant(depositionId);

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedMessage, result.Errors.Select(x => x.Message));
        }

        [Fact]
        public async Task ManageChatParticipant_ShouldFail_CreateChat()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(x => x.Id == depositionId, It.IsAny<string[]>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new Deposition());
            _twilioServiceMock.Setup(x => x.CreateChat(It.IsAny<string>())).ReturnsAsync(Result.Fail($"Error creating chat with name: {depositionId}"));
            var expectedMessage = $"Error creating chat with name: {depositionId}";

            //Act
            var result = await serviceToTest.ManageChatParticipant(depositionId);

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedMessage, result.Errors.Select(x => x.Message));
        }

        [Fact]
        public async Task ManageChatParticipant_ShouldFail_ParticipantNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var user = new User { Id = Guid.NewGuid() };
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>()
            };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(x => x.Id == depositionId, It.IsAny<string[]>(),
                It.IsAny<bool>()))
                .ReturnsAsync(deposition);
            _twilioServiceMock.Setup(x => x.CreateChat(It.IsAny<string>())).ReturnsAsync(Result.Ok(Guid.NewGuid().ToString()));
            var expectedMessage = $"Participant not found with USER ID: {user.Id}";

            //Act
            var result = await serviceToTest.ManageChatParticipant(depositionId);

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedMessage, result.Errors.Select(x => x.Message));
            _depositionRepositoryMock.Verify(x => x.Update(It.IsAny<Deposition>()), Times.Once);
        }

        [Fact]
        public async Task ManageChatParticipant_ShouldFail_CreateChatUser()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "test@email.com" };
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() { new Participant { User = user, UserId = user.Id } }
            };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(x => x.Id == depositionId, It.IsAny<string[]>(),
                It.IsAny<bool>()))
                .ReturnsAsync(deposition);
            _twilioServiceMock.Setup(x => x.CreateChat(It.IsAny<string>())).ReturnsAsync(Result.Ok(Guid.NewGuid().ToString()));
            _twilioServiceMock.Setup(x => x.CreateChatUser(It.IsAny<TwilioIdentity>())).ReturnsAsync(Result.Fail($"Error creating user with identity: {user.EmailAddress}"));
            var expectedMessage = $"Error creating user with identity: {user.EmailAddress}";

            //Act
            var result = await serviceToTest.ManageChatParticipant(depositionId);

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedMessage, result.Errors.Select(x => x.Message));
            _depositionRepositoryMock.Verify(x => x.Update(It.IsAny<Deposition>()), Times.Once);
        }

        [Fact]
        public async Task ManageChatParticipant_Should_OK()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var user = new User { Id = Guid.NewGuid(), EmailAddress = "test@email.com" };
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() { new Participant { User = user, UserId = user.Id } }
            };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(x => x.Id == depositionId, It.IsAny<string[]>(),
                It.IsAny<bool>()))
                .ReturnsAsync(deposition);
            _twilioServiceMock.Setup(x => x.CreateChat(It.IsAny<string>())).ReturnsAsync(Result.Ok(Guid.NewGuid().ToString()));
            _twilioServiceMock.Setup(x => x.CreateChatUser(It.IsAny<TwilioIdentity>())).ReturnsAsync(Result.Ok());

            //Act
            var result = await serviceToTest.ManageChatParticipant(depositionId);

            //Assert
            Assert.True(result.IsSuccess);
            _depositionRepositoryMock.Verify(x => x.Update(It.IsAny<Deposition>()), Times.Once);
            _twilioServiceMock.Verify(x => x.AddUserToChat(It.IsAny<string>(), It.IsAny<TwilioIdentity>(), It.IsAny<string>()), Times.Once);
        }
    }
}
