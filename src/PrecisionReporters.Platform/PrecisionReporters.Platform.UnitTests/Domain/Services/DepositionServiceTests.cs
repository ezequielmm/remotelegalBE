using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
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
        // TODO: we need to refactor this file to have the test setup on the constructor
        private readonly DepositionService _depositionService;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;

        private readonly List<Deposition> _depositions = new List<Deposition>();

        public DepositionServiceTests()
        {
            // Setup
            _depositionRepositoryMock = new Mock<IDepositionRepository>();

            _depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _userServiceMock = new Mock<IUserService>();

            _roomServiceMock = new Mock<IRoomService>();

            _depositionService = new DepositionService(_depositionRepositoryMock.Object, _userServiceMock.Object, _roomServiceMock.Object);
        }

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
            var depositionDocuments = DepositionFactory.GetDocumentList();


            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(deposition.Requester));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, depositionDocuments, deposition.Requester);

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
            var depositionDocuments = DepositionFactory.GetDocumentList();
            var errorMessage = $"Requester with email {deposition.Requester.EmailAddress} not found";

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error("Mocked error")));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, depositionDocuments, null);

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
            var result = await service.GenerateScheduledDeposition(deposition, null, null);

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
            var captionDocument = new Document
            {
                FileKey = fileKey
            };

            var documents = DepositionFactory.GetDocumentList();
            documents.Add(captionDocument);

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.GenerateScheduledDeposition(deposition, documents, null);

            //Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(captionDocument, result.Value.Caption);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnAllDepositions_WhenStatusParameterIsNull()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());


            var depositionRepositoryMock = new Mock<IDepositionRepository>();

            depositionRepositoryMock.Setup(x => x.GetByStatus(
                It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(_depositions.FindAll(x => x.Status == DepositionStatus.Pending));

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User { IsAdmin = true }));

            var service = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);


            // Act
            var result = await service.GetDepositionsByStatus(null, null, null, "fake_user@mail.com");

            Assert.NotEmpty(result);
            depositionRepositoryMock.Verify(r => r.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.Is<Expression<Func<Deposition, bool>>>((x => x != null)),
                It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnPendingDepositions_WhenStatusParameterIsPending()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());


            var depositionRepositoryMock = new Mock<IDepositionRepository>();

            depositionRepositoryMock.Setup(x => x.GetByStatus(
                It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(_depositions.FindAll(x => x.Status == DepositionStatus.Pending));

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User { IsAdmin = true }));

            var service = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);

            // Act
            var result = await service.GetDepositionsByStatus(DepositionStatus.Pending, null, null, "fake_user@mail.com");

            depositionRepositoryMock.Verify(r => r.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.Is<Expression<Func<Deposition, bool>>>(x => x != null),
                It.IsAny<string[]>()), Times.Once);
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
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);
            // Act
            var result = await depositionService.JoinDeposition(depositionId, identity);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            Assert.Equal(result.Errors[0].Message, errorMessage);
            Assert.True(result.IsFailed);
            Assert.NotNull(deposition.TimeZone);
            Assert.Equal("EST", deposition.TimeZone);
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
            Assert.NotNull(deposition.TimeZone);
            Assert.Equal("EST", deposition.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnJoinDepositionInfo_WhenDepositionWithoutWitnessIdExist()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var identity = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithoutWitness(depositionId, caseId);
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
            Assert.Null(result.Value.WitnessEmail);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
        }

        [Fact]
        public async Task EndDeposition_ShouldReturnError_WhenDepositionIdDoesNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            var errorMessage = $"Deposition with id {depositionId} not found.";
            var identity = Guid.NewGuid().ToString();

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock);
            // Act
            var result = await depositionService.EndDeposition(depositionId);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());

            Assert.Equal(result.Errors[0].Message, errorMessage);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task EndDeposition_ShouldReturnDepositionDto_WhenDepositionIdExist()
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
            roomServiceMock.Setup(x => x.EndRoom(It.IsAny<Room>())).ReturnsAsync(() => Result.Ok(new Room()));

            var depositionService = InitializeService(depositionRepository: depositionRepositoryMock, roomService: roomServiceMock);

            // Act
            var result = await depositionService.EndDeposition(depositionId);

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Status == DepositionStatus.Complete && d.CompleteDate.HasValue)), Times.Once());
            roomServiceMock.Verify(mock => mock.EndRoom(It.IsAny<Room>()), Times.Once());
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GoOnRecord_ShouldReturnOnRecordTrue_WhenOnRecordIsTrue()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);

            var IsOnRecord = true;
            deposition.IsOnTheRecord = !IsOnRecord;
            _depositions.Add(deposition);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var depositionService = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Once());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsOnTheRecord);
            Assert.NotEmpty(result.Value.Events);
        }

        [Fact]
        public async Task GoOnRecord_ShouldFail_WhenOnRecordParameterIsTheSameAsCurrentValue()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);

            var IsOnRecord = true;
            deposition.IsOnTheRecord = !IsOnRecord;
            deposition.IsOnTheRecord = IsOnRecord;
            _depositions.Add(deposition);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var depositionService = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Never());

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GoOnRecord_ShouldReturnOnRecordFalse_WhenOnRecordIsFalse()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var IsOnRecord = false;
            deposition.IsOnTheRecord = !IsOnRecord;
            _depositions.Add(deposition);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var depositionService = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);

            // Act
            var result = await depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Once());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(!result.Value.IsOnTheRecord);
            Assert.NotEmpty(result.Value.Events);
        }

        [Fact]
        public async Task AddEvent_ShouldReturnADepositionWithAEvent_WhenAEventIsAdded()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);

            var depositionRepositoryMock = new Mock<IDepositionRepository>();
            depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var depositionService = InitializeService(userService: userServiceMock, depositionRepository: depositionRepositoryMock);

            var depositionEvent = new DepositionEvent
            {
                CreationDate = DateTime.UtcNow,
                EventType = EventType.EndDeposition
            };

            // Act
            var result = await depositionService.AddDepositionEvent(depositionId, depositionEvent, "user@mail.com");

            // Assert
            depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Events[0].EventType == EventType.EndDeposition)), Times.Once());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value.Events);
        }

        [Fact]
        public async Task Update_ShouldReturnFail_IfDepositionNotFound()
        {
            var deposition = new Deposition { Id = Guid.NewGuid() };
            // Arrange
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.Update(deposition);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == deposition.Id), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task Update_ShouldReturnOk()
        {
            // Arrange
            var deposition = new Deposition { Id = Guid.NewGuid(), SharingDocumentId = Guid.NewGuid() };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.Update(deposition);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == deposition.Id), It.IsAny<string[]>()), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(a => a == deposition)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetSharedDocument_ShouldReturnFail_IDepositionFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = "Desosition not found";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.GetSharedDocument(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a=>a.Contains(nameof(Deposition.SharingDocument)))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetSharedDocument_ShouldReturnFail_IfNoDocumentBeingShared()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId };
            var expectedError = "No document is being shared in this deposition";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.GetSharedDocument(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a=>a.Contains(nameof(Deposition.SharingDocument)))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetSharedDocument_ShouldReturnOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var document = new Document { Id = Guid.NewGuid() };
            var deposition = new Deposition { Id = depositionId, SharingDocumentId = document.Id, SharingDocument= document};

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.GetSharedDocument(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a=>a.Contains(nameof(Deposition.SharingDocument)))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(deposition.SharingDocument, result.Value);
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
