﻿using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using Xunit;
using PrecisionReporters.Platform.Domain.Mappers;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class ParticipantServiceTests
    {
        private readonly ParticipantService _participantService;
        private readonly Mock<IParticipantRepository> _participantRepositoryMock;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IDeviceInfoRepository> _deviceInfoRepositoryMock;
        private readonly Mock<ISignalRDepositionManager> _signalRNotificationManagerMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;
        private readonly Mock<IDepositionEmailService> _depositionEmailServiceMock;
        private readonly Mock<IMapper<Participant, ParticipantDto, CreateParticipantDto>> _participantMapperMock;
        private readonly Mock<IParticipantValidationHelper> _participantValidatorHelper;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<ILogger<ParticipantService>> _loggerMock;
        public ParticipantServiceTests()
        {
            _participantRepositoryMock = new Mock<IParticipantRepository>();
            _signalRNotificationManagerMock = new Mock<ISignalRDepositionManager>();
            _userServiceMock = new Mock<IUserService>();
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _depositionEmailServiceMock = new Mock<IDepositionEmailService>();
            _participantMapperMock = new Mock<IMapper<Participant, ParticipantDto, CreateParticipantDto>>();
            _deviceInfoRepositoryMock = new Mock<IDeviceInfoRepository>();
            _participantValidatorHelper = new Mock<IParticipantValidationHelper>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<ParticipantService>>();
            _participantService = new ParticipantService(_participantRepositoryMock.Object,
                _signalRNotificationManagerMock.Object,
                _userServiceMock.Object,
                _depositionRepositoryMock.Object,
                _deviceInfoRepositoryMock.Object,
                _permissionServiceMock.Object,
                _depositionEmailServiceMock.Object,
                _participantMapperMock.Object,
                _participantValidatorHelper.Object,
                _roomServiceMock.Object,
                _loggerMock.Object);
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
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync(participant);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
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
            var errorMessage = "There are no participant available with such userId and depositionId combination.";
            var user = ParticipantFactory.GetNotAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
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
            var errorMessage = "There was an error creating a new Participant.";
            var user = ParticipantFactory.GetAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Participant)null);
            _participantRepositoryMock.Setup(x => x.Create(It.IsAny<Participant>()))
                 .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
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
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Participant)null);
            _participantRepositoryMock.Setup(x => x.Create(It.IsAny<Participant>()))
                 .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                 .ReturnsAsync(participant);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Create(It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);

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
            var errorMessage = $"There was an error updating Participant with Id:";
            var user = ParticipantFactory.GetNotAdminUser();
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            var participant = ParticipantFactory.GetParticipant(depositionId);

            _userServiceMock.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                 .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
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
            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(participant);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>()))
                 .ReturnsAsync(participant);
            _signalRNotificationManagerMock.Setup(x => x.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()))
                .Throws(new Exception("SignalR exception"));

            //Act
            var result = await _participantService.UpdateParticipantStatus(participantStatus, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
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

        [Fact]
        public async Task RemoveParticipant_ShouldReturnFail_IfDepositionNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var participantId = Guid.NewGuid();
            var expectedError = $"Deposition not found with ID {depositionId}";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);
            _depositionEmailServiceMock.Setup(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()));

            // Act
            var result = await _participantService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Case), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
            _depositionEmailServiceMock.Verify(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()), Times.Never);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveParticipant_ShouldReturnFail_IfParticipantNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var mockDeposition = new Deposition()
            {
                Id = depositionId,
                Participants = new List<Participant>()
                {
                    new Participant()
                    {
                        Id = Guid.NewGuid()
                    }
                }
            };
            var participantId = Guid.NewGuid();
            var expectedError = $"Participant not found with ID {participantId}";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(mockDeposition);
            _depositionEmailServiceMock.Setup(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()));

            // Act
            var result = await _participantService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Case), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
            _depositionEmailServiceMock.Verify(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()), Times.Never);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveParticipant_ShouldReturnOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var participantId = Guid.NewGuid();
            var mockDeposition = new Deposition()
            {
                Id = depositionId,
                Participants = new List<Participant>()
                {
                    new Participant()
                    {
                        Id = participantId
                    }
                }
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(mockDeposition);
            _depositionEmailServiceMock.Setup(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()));

            // Act
            var result = await _participantService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Case), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
            _permissionServiceMock.Verify(x => x.RemoveParticipantPermissions(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Remove(It.IsAny<Participant>()));
            _depositionEmailServiceMock.Verify(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()), Times.Never);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldReturnOk()
        {
            // Arrange
            var newUser = ParticipantFactory.GetNotAdminUser();
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Id = baseParticipant.Id,
                Email = "newparticipant@mail.com",
                Name = "Participant Name",
                Role = baseParticipant.Role,
                User = newUser,
                UserId = newUser.Id };

            var user = ParticipantFactory.GetAdminUser();

            _userServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _userServiceMock
                .Setup(x => x.GetUserByEmail(editedParticipant.Email))
                .ReturnsAsync(Result.Ok(newUser));
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, Participants = new List<Participant> { baseParticipant } });

            _participantRepositoryMock
                .Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync(editedParticipant);

            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);
            _permissionServiceMock.Verify(x => x.RemoveParticipantPermissions(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count == 0);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldNotChangeNameAndLastName_WhenIsRegisteredUser()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var newUser = ParticipantFactory.GetNotAdminUser();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.User = newUser;
            baseParticipant.Email = "base@email.com";
            var editedParticipant = new Participant
            {
                Id = baseParticipant.Id,
                Email = $"editedemail@mail.com",
                Name = "Participant Name",
                LastName = "Participant LastName",
                Role = baseParticipant.Role,
                User = null
            };
            var expectedParticipant = new Participant();
            expectedParticipant.CopyFrom(baseParticipant);
            expectedParticipant.Email = editedParticipant.Email;
            expectedParticipant.Role = editedParticipant.Role;
            expectedParticipant.Name = newUser.FirstName;
            expectedParticipant.LastName = newUser.LastName;

            _userServiceMock
                .Setup(x => x.GetUserByEmail(editedParticipant.Email))
                .ReturnsAsync(Result.Ok(newUser));
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, Participants = new List<Participant> { baseParticipant } });
            _participantRepositoryMock
                .Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync(expectedParticipant);

            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(newUser.FirstName, result.Value.Name);
            Assert.Equal(newUser.LastName, result.Value.LastName);
        }
        
        [Fact]
        public async Task RemoveParticipant_ShouldSendEmail_WhenStatusIsConfirmed()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var participantId = Guid.NewGuid();
            var mockDeposition = new Deposition()
            {
                Id = depositionId,
                Participants = new List<Participant>()
                {
                    new Participant()
                    {
                        Id = participantId,
                        Email = "participant@test.com"
                    }
                },
                Status = DepositionStatus.Confirmed
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(mockDeposition);
            _depositionEmailServiceMock.Setup(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()));

            // Act
            var result = await _participantService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Case), $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
            _permissionServiceMock.Verify(x => x.RemoveParticipantPermissions(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Remove(It.IsAny<Participant>()));
            _depositionEmailServiceMock.Verify(x => x.SendCancelDepositionEmailNotification(It.IsAny<Deposition>(), It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldReturnResourceNotFoundError_WhenParticipantNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "There are no participant available with ID:";
            var user = ParticipantFactory.GetNotAdminUser();
            var editedParticipant = ParticipantFactory.GetParticipant(depositionId);
            editedParticipant.User = new User { IsGuest = false };

            _userServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, Participants = new List<Participant> { ParticipantFactory.GetParticipant(depositionId) } });

            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Never);

            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldReturnResourceNotFoundError_WhenFailsToUpdateParticipant()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.User = new User { IsGuest = false };

            var editedParticipant = new Participant { Id = baseParticipant.Id, Email = null, Name = null, Role = baseParticipant.Role };
            var errorMessage = $"There was an error updating Participant with Id:";
            var user = ParticipantFactory.GetNotAdminUser();

            _userServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, Participants = new List<Participant> { baseParticipant } });
            _participantRepositoryMock
                .Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);

            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldReturnResourceNotFoundError_WhenDepositionNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            var editedParticipant = new Participant { Id = baseParticipant.Id, Email = null, Name = null, Role = baseParticipant.Role };
            var errorMessage = "Deposition not found with ID:";
            var user = ParticipantFactory.GetNotAdminUser();

            _userServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync((Deposition)null);

            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);
            _participantRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task EditParticipantDetails_ShouldReturnResourceConflictError_WhenMoreThanOneCourtReporter()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var courtReporterParticipant = ParticipantFactory.GetParticipant(depositionId);
            courtReporterParticipant.Role = ParticipantType.CourtReporter;
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.Role = ParticipantType.CourtReporter;
            var editedParticipant = new Participant { Id = baseParticipant.Id, Email = null, Name = null, Role = baseParticipant.Role };
            var errorMessage = "Only one participant with Court reporter role is available.";
            var user = ParticipantFactory.GetNotAdminUser();

            _userServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, Participants = new List<Participant> { courtReporterParticipant } });


            //Act
            var result = await _participantService.EditParticipantDetails(depositionId, editedParticipant);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case) }))), Times.Once);

            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Theory]
        [InlineData(ParticipantType.Witness)]
        [InlineData(ParticipantType.CourtReporter)]
        [InlineData(ParticipantType.Attorney)]
        [InlineData(ParticipantType.Interpreter)]
        [InlineData(ParticipantType.Paralegal)]
        [InlineData(ParticipantType.TechExpert)]
        public async Task EditParticipantInDepo_ShouldReturnOk_WhenIsOffTheRecord(ParticipantType participantType)
        {
            // Arrange
            var baseRole = participantType;
            var depositionId = Guid.NewGuid();
            var targetParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.Observer);
            targetParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Id = targetParticipant.Id, Role = baseRole, Email = targetParticipant.Email};
            editedParticipant.User = targetParticipant.User;
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant> { targetParticipant }, IsOnTheRecord = false };
            var amazingTwilioToken = Guid.NewGuid().ToString();
            _participantValidatorHelper
                .Setup(mock => mock.GetValidDepositionForEditParticipantAsync(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));
            _participantValidatorHelper
                .Setup(mock => mock.ValidateTargetParticipantForEditRole(It.IsAny<Deposition>(), It.IsAny<Participant>(), targetParticipant))
                .Returns(Result.Ok());
            _participantRepositoryMock
                .Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync(editedParticipant);
            _roomServiceMock
                .Setup(mock => mock.RefreshRoomToken(It.IsAny<Participant>(), It.IsAny<Deposition>()))
                .ReturnsAsync(amazingTwilioToken);
            _participantRepositoryMock
                .Setup(mock => mock.GetByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Participant> { targetParticipant });
            //Act
            var result = await _participantService.EditParticipantInDepo(depositionId, editedParticipant);

            // Assert
            _participantValidatorHelper.Verify(mock => mock.GetValidDepositionForEditParticipantAsync(It.IsAny<Guid>()), Times.Once);
            _participantValidatorHelper.Verify(mock => mock.ValidateTargetParticipantForEditRole(It.IsAny<Deposition>(), It.IsAny<Participant>(), It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);
            _signalRNotificationManagerMock.Verify(mock => mock.SendDirectMessage(It.IsAny<string>(), It.IsAny<NotificationDto>()), Times.Once);
            _roomServiceMock.Verify(mock => mock.RefreshRoomToken(It.IsAny<Participant>(), It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count == 0);
        }

        [Fact]
        public async Task EditParticipantInDepo_ShouldReturnInvalidInputError_WhenFailsToUpdateParticipant()
        {
            var depositionId = Guid.NewGuid();
            var errorMessage = "There was an error updating Participant with Id:";
            var targetParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.Witness);
            targetParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Id = targetParticipant.Id, Role = ParticipantType.Attorney, Email = targetParticipant.Email};
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant> { targetParticipant }, IsOnTheRecord = false };
            _participantValidatorHelper
                .Setup(mock => mock.GetValidDepositionForEditParticipantAsync(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));
            _participantValidatorHelper
                .Setup(mock => mock.ValidateTargetParticipantForEditRole(It.IsAny<Deposition>(), It.IsAny<Participant>(), targetParticipant))
                .Returns(Result.Ok(targetParticipant));
            _participantRepositoryMock
                .Setup(x => x.Update(It.IsAny<Participant>()))
                .ReturnsAsync((Participant)null);

            //Act
            var result = await _participantService.EditParticipantInDepo(depositionId, editedParticipant);

            // Assert
            _participantValidatorHelper.Verify(mock => mock.GetValidDepositionForEditParticipantAsync(It.IsAny<Guid>()), Times.Once);
            _participantValidatorHelper.Verify(mock => mock.ValidateTargetParticipantForEditRole(It.IsAny<Deposition>(), It.IsAny<Participant>(), It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Update(It.IsAny<Participant>()), Times.Once);
            _signalRNotificationManagerMock.Verify(mock => mock.SendDirectMessage(It.IsAny<string>(), It.IsAny<NotificationDto>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }
    }
}
