﻿using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DepositionServiceTests : IDisposable
    {
        private readonly List<Deposition> _depositions = new List<Deposition>();        

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task GetDepositions_ShouldReturn_ListOfAllDepositions()
        {
            // Arrange
            var depositions = DepositionFactory.GetDepositionList();
            _depositions.AddRange(depositions);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GetDepositions();

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.NotEmpty(result);
            Assert.Equal(_depositions.Count, result.Count);
        }

        [Fact]
        public async Task GetDepositionById_ShouldReturn_DepositionWithGivenId()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GetDepositionById(depositionId);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            Assert.True(result.IsSuccess);

            var foundDeposition = result.Value;
            Assert.NotNull(foundDeposition);
            Assert.Equal(depositionId, foundDeposition.Id);
        }

        [Fact]
        public async Task GetDepositionById_ShouldReturnError_WhenDepositionIdDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var errorMessage = $"Deposition with id {id} not found.";

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GetDepositionById(id);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()), Times.Once());
            Assert.Equal(result.Errors[0].Message, errorMessage);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldReturn_NewDeposition()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var depositionDocuments = DepositionFactory.GetDepositionDocumentList();


            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(deposition.Requester));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, depositionDocuments);

            // Assert
            userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == deposition.Requester.EmailAddress)), Times.Once());
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldReturnError_WhenEmailAddressIsNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var depositionDocuments = DepositionFactory.GetDepositionDocumentList();
            var errorMessage = $"Requester with email {deposition.Requester.EmailAddress} not found";

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error ("Mocked error")));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, depositionDocuments);

            // Assert
            userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == deposition.Requester.EmailAddress)), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldCall_GetUserByEmail_WhenWitnessEmailNotEmpty()
        {
            // Arrange
            var witnessEmail = "testWitness@mail.com";
            var deposition = new Deposition
            {
                Witness = new Participant
                {
                    Email = witnessEmail
                },
                Requester = new User
                {
                    EmailAddress = "requester@email.com"
                }
            };


            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, null);

            // Assert
            userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == witnessEmail)), Times.Once());
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldTakeCaptionFile_WhenDepositionFileKeyNotEmpty()
        {
            // Arrange
            var fileKey = "TestFileKey";
            var deposition = new Deposition
            {
                Requester = new User
                {
                    EmailAddress = "requester@email.com"
                },
                FileKey = fileKey
            };
            var captionDocument = new DepositionDocument
            {
                FileKey = fileKey
            };

            var documents = DepositionFactory.GetDepositionDocumentList();
            documents.Add(captionDocument);

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, documents);

            //Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(captionDocument, result.Value.Caption);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnError_WhenDepositionIdDoesNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            var errorMessage = $"Deposition with id {depositionId} not found.";
            var identity = Guid.NewGuid().ToString();

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition) null);

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);
            // Act
            var result = await depositionService.JoinDeposition(depositionId, identity);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            Assert.Equal(result.Errors[0].Message, errorMessage);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnJoinDepositionInfo_WhenDepositionIdExist()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var identity = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            
            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var roomServiceMock = new Mock<IRoomService>();
            roomServiceMock.Setup(x => x.StartRoom(It.IsAny<Room>())).Verifiable();         
            roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(token));

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock, roomService: roomServiceMock);

            // Act
            var result = await depositionService.JoinDeposition(depositionId, identity);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        private DepositionService InitializeService(
            Mock<IDepositionRepository> depositionRepository = null,
            Mock<IUserService> userService = null,
            Mock<IRoomService> roomService = null)
        {

            var depositionRepositoryMock = depositionRepository ?? new Mock<IDepositionRepository>();
            var userServiceMock = userService ?? new Mock<IUserService>();
            var roomServiceMock = roomService ?? new Mock<IRoomService>();

            return new DepositionService(
                depositionRepositoryMock.Object,
                userServiceMock.Object,
                roomServiceMock.Object                
                );
        }
    }
}