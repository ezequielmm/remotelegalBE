using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class ParticipantServiceTests
    {
        private readonly ParticipantService _participantService;
        private readonly Mock<IParticipantRepository> _participantRepositoryMock;
        private readonly Mock<ISignalRNotificationManager> _signalRNotificationManagerMock;
        private readonly Mock<IUserService> _userServiceMock;
        public ParticipantServiceTests()
        {
            _participantRepositoryMock = new Mock<IParticipantRepository>();
            _signalRNotificationManagerMock = new Mock<ISignalRNotificationManager>();
            _userServiceMock = new Mock<IUserService>();

            _participantService = new ParticipantService(_participantRepositoryMock.Object, _signalRNotificationManagerMock.Object, _userServiceMock.Object);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);
            var user = ParticipantFactory.GetAdminUser();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync(participant);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Never);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result);
            Assert.IsType<Result<ParticipantStatusDto>>(result);
            Assert.True(result.Errors.Count == 0);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnNullParticipantException_WhenUserIsNotAdmin()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "The are no participant available with such userId and depositionId combination.";
            var user = ParticipantFactory.GetNotAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Never);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Never);

            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnUnexpectedException_WhenFailsToCreateNewParticipantAdmin()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "The was an error creating a new Participant.";
            var user = ParticipantFactory.GetAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Participant)null);
            _participantRepositoryMock.Setup(x => x.Create(It.IsAny<Participant>()))
                 .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Never);

            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnOk_WhenNewParticipantAdminIsCreated()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var user = ParticipantFactory.GetAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Participant)null);
            _participantRepositoryMock.Setup(x => x.Create(It.IsAny<Participant>()))
                 .ReturnsAsync(participant);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Never);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result);
            Assert.IsType<Result<ParticipantStatusDto>>(result);
            Assert.True(result.Errors.Count == 0);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnUnexpectedException_WhenFailsToUpdateParticipant()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = $"The was an error updating Participant with Id:";
            var user = ParticipantFactory.GetNotAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                 .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Never);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);

            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task UpdateParticipantStatus_ShouldReturnUnexpectedException_WhenSignalRFailsToSendMessage()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var user = ParticipantFactory.GetNotAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                 .ReturnsAsync(participant);
            _signalRNotificationManagerMock.Setup(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()))
                .Throws(new Exception("SignalR exception"));

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Never);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);

            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task GetWaitingRoomParticipants_ShouldOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var participantList = new List<Participant>()
            {
                new Participant(){Id= Guid.NewGuid() },
                new Participant(){Id= Guid.NewGuid() },
                new Participant(){Id= Guid.NewGuid() },
                new Participant(){Id= Guid.NewGuid() },
                new Participant(){Id= Guid.NewGuid() },
            };
            _participantRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(participantList);

            //Act
            var result = await _participantService.GetWaitParticipants(depositionId);

            //Assert
            _participantRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.True(participantList.Count == result.Value.Count);
            Assert.IsType<Participant>(result.Value[0]);
        }

    }
}
