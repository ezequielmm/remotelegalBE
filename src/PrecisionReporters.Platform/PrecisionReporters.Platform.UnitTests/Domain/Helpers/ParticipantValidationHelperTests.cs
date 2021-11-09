using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Helpers;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Helpers
{
    public class ParticipantValidationHelperTests
    {
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly IParticipantValidationHelper _classUnderTest;

        public ParticipantValidationHelperTests()
        {
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _classUnderTest = new ParticipantValidationHelper(_depositionRepositoryMock.Object);
        }

        [Fact]
        public async Task GetValidDepositionForEditParticipantAsync_ShouldReturnResourceNotFoundError_WhenDepositionNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "Deposition not found with ID:";
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync((Deposition)null);

            //Act
            var result = await _classUnderTest.GetValidDepositionForEditParticipantAsync(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(
            mock => mock.GetById(It.IsAny<Guid>(),
            It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case), nameof(Deposition.Events), nameof(Deposition.Room) }))
            ), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task GetValidDepositionForEditParticipantAsync_ShouldReturnResourceInvalidInputError_WhenIsOnTheRecord()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.Role = ParticipantType.Observer;
            var errorMessage = "IsOnTheRecord A participant edit cannot be made if Deposition is currently on the record.";
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, IsOnTheRecord = true });

            //Act
            var result = await _classUnderTest.GetValidDepositionForEditParticipantAsync(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(
            mock => mock.GetById(It.IsAny<Guid>(),
            It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case), nameof(Deposition.Events), nameof(Deposition.Room) }))
            ), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task GetValidDepositionForEditParticipantAsync_ShouldReturnOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            baseParticipant.Role = ParticipantType.Observer;
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { Id = depositionId, IsOnTheRecord = false });

            //Act
            var result = await _classUnderTest.GetValidDepositionForEditParticipantAsync(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(
            mock => mock.GetById(It.IsAny<Guid>(),
            It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}", nameof(Deposition.Case), nameof(Deposition.Events), nameof(Deposition.Room) }))
            ), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(depositionId, result.Value.Id);
        }

        [Fact]
        public void ValidateTargetParticipantForEditRole_ShouldReturnResourceInvalidInputError_WhenMoreThanOneCourtReporter()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "Only one participant with Court reporter role is allowed.";
            var courtReporterParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.CourtReporter);
            var baseParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.CourtReporter);
            var editedParticipant = new Participant { Id = baseParticipant.Id, Role = baseParticipant.Role };
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant> { courtReporterParticipant } };

            //Act
            var result = _classUnderTest.ValidateTargetParticipantForEditRole(deposition, editedParticipant, courtReporterParticipant);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }
        
        [Fact]
        public void ValidateTargetParticipantForEditRole_ShouldReturnInvalidInputError_WhenRoleItSame()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var baseParticipant = ParticipantFactory.GetParticipant(depositionId);
            var editedParticipant = new Participant { Email = baseParticipant.Email, Role = baseParticipant.Role };
            var errorMessage = "Participant already have requested role.";
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant> { baseParticipant } };

            //Act
            var result = _classUnderTest.ValidateTargetParticipantForEditRole(deposition, editedParticipant, baseParticipant);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        // This is a restriction for this increment. Once the depo was on the record, witness cannot be exchanged
        [Fact]
        public void ValidateTargetParticipantForEditRole_ShouldReturnResourceInvalidInputError_WhenDepositionWasOnTheRecordAndTryChangeWitness()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "HasBeenOnTheRecord A Witness participant cannot be exchanged if Deposition has been on the record.";
            var events = new DepositionEvent { EventType = EventType.OnTheRecord };
            var targetParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.Witness);
            targetParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Email = targetParticipant.Email, Role = ParticipantType.Attorney };
            var deposition = new Deposition
            {
                Id = depositionId, Participants = new List<Participant> { targetParticipant }, IsOnTheRecord = false, Events = new List<DepositionEvent> { events }
            };

            //Act
            var result = _classUnderTest.ValidateTargetParticipantForEditRole(deposition, editedParticipant, targetParticipant);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }
        
        [Fact]
        public void ValidateTargetParticipantForEditRole_ShouldReturnResourceInvalidInputError_WhenDepositionWasOnTheRecordAndTryChangeParticipantToWitness()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var errorMessage = "HasBeenOnTheRecord A Witness participant cannot be exchanged if Deposition has been on the record.";
            var events = new DepositionEvent { EventType = EventType.OnTheRecord };
            var targetParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.Attorney);
            targetParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Email = targetParticipant.Email, Role = ParticipantType.Witness };
            var deposition = new Deposition
            {
                Id = depositionId, Participants = new List<Participant> { targetParticipant }, IsOnTheRecord = false, Events = new List<DepositionEvent> { events }
            };

            //Act
            var result = _classUnderTest.ValidateTargetParticipantForEditRole(deposition, editedParticipant, targetParticipant);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }
        
        [Fact]
        public void GetValidTargetParticipantForEditRole_ShouldReturnOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var targetParticipant = ParticipantFactory.GetParticipantByGivenRole(ParticipantType.Observer);
            targetParticipant.User = new User { IsGuest = false };
            var editedParticipant = new Participant { Id = targetParticipant.Id, Role = ParticipantType.Witness };
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant> { targetParticipant }, IsOnTheRecord = false };

            //Act
            var result = _classUnderTest.ValidateTargetParticipantForEditRole(deposition, editedParticipant, targetParticipant);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(editedParticipant.Id, targetParticipant.Id);
        }
    }
}