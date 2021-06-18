using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class ParticipantsControllerTest
    {
        private readonly Mock<IParticipantService> _participantService;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Participant, EditParticipantDto, object> _editParticipantMapper;

        private readonly ParticipantsController _classUnderTest;

        public ParticipantsControllerTest()
        {
            _participantService = new Mock<IParticipantService>();
            _participantMapper = new ParticipantMapper();
            _editParticipantMapper = new EditParticipantMapper();
            _classUnderTest = new ParticipantsController(_participantService.Object, _participantMapper, _editParticipantMapper);
        }

        [Fact]
        public async Task ParticipantStatus_ReturnsOkAndParticipantStatusDto()
        {
            // Arrange
            var participantStatus = ParticipantFactory.GetParticipantSatus();
            _participantService
                .Setup(mock => mock.UpdateParticipantStatus(It.IsAny<ParticipantStatusDto>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(participantStatus));

            // Act
            var result =await _classUnderTest.ParticipantStatus(It.IsAny<Guid>(), It.IsAny<ParticipantStatusDto>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<ParticipantStatusDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<ParticipantStatusDto>(okResult.Value);
            Assert.Equal(participantStatus.Email, resultValue.Email);
            Assert.Equal(participantStatus.IsMuted, resultValue.IsMuted);
            _participantService.Verify(mock=>mock.UpdateParticipantStatus(It.IsAny<ParticipantStatusDto>(), It.IsAny<Guid>()),Times.Once);
        }

        [Fact]
        public async Task ParticipantStatus_ReturnsError_WhenUpdateParticipantStatusFails()
        {
            // Arrange
            _participantService
                .Setup(mock => mock.UpdateParticipantStatus(It.IsAny<ParticipantStatusDto>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result =await _classUnderTest.ParticipantStatus(It.IsAny<Guid>(), It.IsAny<ParticipantStatusDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _participantService.Verify(mock=>mock.UpdateParticipantStatus(It.IsAny<ParticipantStatusDto>(), It.IsAny<Guid>()),Times.Once);
        }

        [Fact]
        public async Task RemoveParticipantFromExistingDeposition_ReturnsOk()
        {
            // Arrange
            _participantService
                .Setup(mock => mock.RemoveParticipantFromDeposition(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result =await _classUnderTest.RemoveParticipantFromExistingDeposition(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _participantService.Verify(mock=>mock.RemoveParticipantFromDeposition(It.IsAny<Guid>(), It.IsAny<Guid>()),Times.Once);
        }
        
        [Fact]
        public async Task RemoveParticipantFromExistingDeposition_ReturnsError_WhenRemoveParticipantFromDepositionFails()
        {
            // Arrange
            _participantService
                .Setup(mock => mock.RemoveParticipantFromDeposition(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result =await _classUnderTest.RemoveParticipantFromExistingDeposition(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _participantService.Verify(mock=>mock.RemoveParticipantFromDeposition(It.IsAny<Guid>(), It.IsAny<Guid>()),Times.Once);
        }

        [Fact]
        public async Task EditParticipant_ReturnsOkAndParticipantDto()
        {
            // Arrange
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            var editParticipantDto = new EditParticipantDto { Id = participant.Id, Role = participant.Role };
            _participantService.Setup(mock => mock.EditParticipantDetails(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Ok(participant));

            // Act
            var result =await _classUnderTest.EditParticipant(It.IsAny<Guid>(),editParticipantDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<ParticipantDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<ParticipantDto>(okResult.Value);
            Assert.Equal(participant.Email, resultValue.Email);
            Assert.Equal(participant.Id, resultValue.Id);
            Assert.Equal(participant.Role.ToString(), resultValue.Role);
            Assert.Equal(participant.Name, resultValue.Name);
            Assert.Equal(participant.IsMuted, resultValue.IsMuted);
            _participantService.Verify(mock=>mock.EditParticipantDetails(It.IsAny<Guid>(), It.IsAny<Participant>()),Times.Once);
        }
        
        [Fact]
        public async Task EditParticipant_ReturnsError_WhenEditParticipantDetailsFails()
        {
            // Arrange
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            var editParticipantDto = new EditParticipantDto { Id = participant.Id, Role = participant.Role };
            _participantService.Setup(mock => mock.EditParticipantDetails(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result =await _classUnderTest.EditParticipant(It.IsAny<Guid>(),editParticipantDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _participantService.Verify(mock=>mock.EditParticipantDetails(It.IsAny<Guid>(), It.IsAny<Participant>()),Times.Once);
        }

    }
}