using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using PrecisionReporters.Platform.Domain.Mappers;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DepositionServiceTests : IDisposable
    {
        // TODO: we need to refactor this file to have the test setup on the constructor
        private readonly DepositionService _depositionService;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IParticipantRepository> _participantRepositoryMock;
        private readonly Mock<IDepositionEventRepository> _depositionEventRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<IBreakRoomService> _breakRoomServiceMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;
        private readonly DocumentConfiguration _documentConfiguration;
        private readonly Mock<IAwsStorageService> _awsStorageServiceMock;
        private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;
        private readonly Mock<IOptions<DocumentConfiguration>> _depositionDocumentConfigurationMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IDocumentService> _documentServiceMock;
        private readonly Mock<ILogger<DepositionService>> _loggerMock;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;

        private readonly List<Deposition> _depositions = new List<Deposition>();

        public DepositionServiceTests()
        {
            // Setup
            _depositionEventRepositoryMock = new Mock<IDepositionEventRepository>();

            _depositionRepositoryMock = new Mock<IDepositionRepository>();

            _participantRepositoryMock = new Mock<IParticipantRepository>();

            _transactionHandlerMock = new Mock<ITransactionHandler>();

            _documentServiceMock = new Mock<IDocumentService>();

            _loggerMock = new Mock<ILogger<DepositionService>>();

            _depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<Deposition, object>>>())).ReturnsAsync(_depositions);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _userServiceMock = new Mock<IUserService>();
            _roomServiceMock = new Mock<IRoomService>();
            _breakRoomServiceMock = new Mock<IBreakRoomService>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _awsStorageServiceMock = new Mock<IAwsStorageService>();
            _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
            _documentConfiguration = new DocumentConfiguration
            {
                PostDepoVideoBucket = "foo"
            };

            _depositionDocumentConfigurationMock = new Mock<IOptions<DocumentConfiguration>>();
            _depositionDocumentConfigurationMock.Setup(x => x.Value).Returns(_documentConfiguration);

            _depositionService = new DepositionService(
                _depositionRepositoryMock.Object,
                _participantRepositoryMock.Object,
                _depositionEventRepositoryMock.Object,
                _userServiceMock.Object,
                _roomServiceMock.Object,
                _breakRoomServiceMock.Object,
                _permissionServiceMock.Object,
                _awsStorageServiceMock.Object,
                _depositionDocumentConfigurationMock.Object,
                _backgroundTaskQueueMock.Object,
                _transactionHandlerMock.Object,
                _loggerMock.Object,
                _documentServiceMock.Object,
                _depositionMapper);

            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<Deposition>>>>()))
                .Returns(async (Func<Task<Result<Deposition>>> action) =>
                {
                    return await action();
                });

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

            _depositionRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositions);
            // Act

            var result = await _depositionService.GetDepositions();

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>()), Times.Once());
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

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            // Act
            var result = await _depositionService.GetDepositionById(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
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

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            // Act
            var result = await _depositionService.GetDepositionById(id);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()), Times.Once());
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
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(deposition.Requester));
            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, depositionDocuments, deposition.Requester);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == deposition.Requester.EmailAddress)), Times.Once());
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
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error("Mocked error")));

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, depositionDocuments, null);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == deposition.Requester.EmailAddress)), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldCall_GetUserByEmail_WhenWitnessEmailNotEmpty()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var witnessEmail = "testWitness@mail.com";
            var deposition = new Deposition
            {
                Participants = new List<Participant> {
                    new Participant { Email = witnessEmail, Role = ParticipantType.Witness }
                },
                Requester = new User
                {
                    EmailAddress = "requester@email.com"
                }
            };
            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User>());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldWork_WithWitnessAndRequesterWithSameEmail()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var witnessEmail = "testWitness@mail.com";
            var deposition = new Deposition
            {
                Participants = new List<Participant> {
                    new Participant { Email = witnessEmail, Role = ParticipantType.Witness }
                },
                Requester = new User
                {
                    EmailAddress = witnessEmail
                }
            };
            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User>());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, null, null);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == witnessEmail)), Times.Once());
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateScheduledDeposition_ShouldTakeCaptionFile_WhenDepositionFileKeyNotEmpty()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var fileKey = "TestFileKey";
            var deposition = new Deposition
            {
                Participants = new List<Participant> {
                    new Participant { Email = "testWitness@mail.com", Role = ParticipantType.Witness }
                },
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

            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User>());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, documents, null);

            //Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(captionDocument, result.Value.Caption);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnAllDepositions_WhenStatusParameterIsNull()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());

            _depositionRepositoryMock.Setup(x => x.GetByStatus(
                It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()))
                .ReturnsAsync(_depositions.FindAll(x => x.Status == DepositionStatus.Pending));

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            // Act
            var result = await _depositionService.GetDepositionsByStatus(null, null, null);

            Assert.NotEmpty(result);
            _depositionRepositoryMock.Verify(r => r.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.Is<Expression<Func<Deposition, bool>>>((x => x != null)),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnPendingDepositions_WhenStatusParameterIsPending()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());
            _depositionRepositoryMock.Setup(x => x.GetByStatus(
                It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()))
                .ReturnsAsync(_depositions.FindAll(x => x.Status == DepositionStatus.Pending));

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            // Act
            var result = await _depositionService.GetDepositionsByStatus(DepositionStatus.Pending, null, null);

            _depositionRepositoryMock.Verify(r => r.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.Is<Expression<Func<Deposition, bool>>>(x => x != null),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionsByStatus_ShouldReturnOrderedDepositionsListByRequester_WhenSortDirectionIsAscendAndSortedFieldIsRequester()
        {
            // Arrange
            var sortedList = DepositionFactory.GetDepositionsWithRequesters().OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName);
            _depositions.AddRange(sortedList);
            _depositionRepositoryMock.Setup(x => x.GetByStatus(
                x => x.Requester.FirstName,
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()))
                .ReturnsAsync(_depositions.OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName).ToList());

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            // Act
            var result = await _depositionService.GetDepositionsByStatus(null, DepositionSortField.Requester, SortDirection.Ascend);

            //Assert
            Assert.Equal(sortedList, result);
        }

        [Fact]
        public async Task GetDepositionsByStatus_ShouldOrderByThen_WhenSortedFieldIsRequester()
        {
            // Arrange
            var sortedList = DepositionFactory.GetDepositionsWithRequesters().OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName);
            _depositions.AddRange(sortedList);
            _depositionRepositoryMock.Setup(x => x.GetByStatus(
                x => x.Requester.FirstName,
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Deposition, object>>>()))
                .ReturnsAsync(_depositions.OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName).ToList());

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            // Act
            var result = await _depositionService.GetDepositionsByStatus(null, DepositionSortField.Requester, SortDirection.Ascend);

            //Assert
            _depositionRepositoryMock.Verify(r => r.GetByStatus(It.IsAny<Expression<Func<Deposition, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<string[]>(),
                It.Is<Expression<Func<Deposition, object>>>(x => x != null)), Times.Once);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnError_WhenUserNotFound()
        {
            // Arrange
            var userEmail = "testing@mail.com";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError()));

            // Act
            var result = await _depositionService.JoinDeposition(Guid.NewGuid(), userEmail);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once());
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnError_WhenDepositionIdDoesNotExist()
        {
            // Arrange
            var userEmail = "testing@mail.com";
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            var errorMessage = $"Deposition with id {depositionId} not found.";

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Room), nameof(Deposition.Participants) }))), Times.Once());
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.NotNull(deposition.TimeZone);
            Assert.Equal("EST", deposition.TimeZone);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnJoinDepositionInfo_WhenDepositionIdExist()
        {
            // Arrange
            var userEmail = "testing@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
            var token = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var currentParticipant = new Participant { Name = "ParticipantName", Role = ParticipantType.Observer, User = user };
            var deposition = new Deposition
            {
                Room = new Room
                {
                    Id = Guid.NewGuid(),
                    Status = RoomStatus.Created,
                    Name = "TestingRoom"
                },
                Participants = new List<Participant>
                {
                   currentParticipant
                },
                TimeZone = "TetingTimeZone",
                IsOnTheRecord = true
            };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room)), Times.Once);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == currentParticipant.Role),
                It.Is<string>(a => a == userEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.TimeZone, result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
        }

        [Fact]
        public async Task JoinDeposition_ShouldJoinAsWitness_WhenParticipantIsWitness()
        {
            // Arrange
            var userEmail = "witness@email.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
            var token = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Room = new Room
                {
                    Id = Guid.NewGuid(),
                    Status = RoomStatus.Created,
                    Name = "TestingRoom"
                },
                Participants = new List<Participant>
                {
                   new Participant { Name = "ParticipantName", Role = ParticipantType.Observer },
                   new Participant { Email = userEmail, Role = ParticipantType.Witness, User = user }
                },
                TimeZone = "TetingTimeZone",
                IsOnTheRecord = true
            };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room)), Times.Once);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == ParticipantType.Witness),
                It.Is<string>(a => a == userEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.TimeZone, result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldReturnError_WhenDepositionDoesNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldReturnError_WhenDepositionIsOnTheRecord()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();
            var deposition = new Deposition { Id = Guid.NewGuid(), IsOnTheRecord = true };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldReturnToken()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();
            var deposition = new Deposition { Id = Guid.NewGuid(), IsOnTheRecord = false };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _breakRoomServiceMock.Setup(x => x.JoinBreakRoom(breakRoomId)).ReturnsAsync(Result.Ok("token"));

            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            Assert.Equal(result.Value, "token");
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
            var userEmail = "currentUser@email.com";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.EndDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());

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
            var userEmail = "currentUser@email.com";
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _roomServiceMock.Setup(x => x.EndRoom(It.IsAny<Room>(), It.IsAny<string>())).ReturnsAsync(() => Result.Ok(new Room()));
            _backgroundTaskQueueMock.Setup(x => x.QueueBackgroundWorkItem(It.IsAny<DraftTranscriptDto>()));

            // Act
            var result = await _depositionService.EndDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Exactly(2));
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Status == DepositionStatus.Completed && d.CompleteDate.HasValue)), Times.Exactly(2));
            _roomServiceMock.Verify(mock => mock.EndRoom(It.IsAny<Room>(), It.IsAny<string>()), Times.Once());
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

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Once());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(EventType.OnTheRecord, result.Value.EventType);
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

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Never());

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

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            // Act
            var result = await _depositionService.GoOnTheRecord(depositionId, IsOnRecord, "user@mail.com");

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.IsOnTheRecord == IsOnRecord)), Times.Once());

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(EventType.OffTheRecord, result.Value.EventType);
        }

        [Fact]
        public async Task AddEvent_ShouldReturnADepositionWithAEvent_WhenAEventIsAdded()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            var depositionEvent = new DepositionEvent
            {
                CreationDate = DateTime.UtcNow,
                EventType = EventType.EndDeposition
            };

            // Act
            var result = await _depositionService.AddDepositionEvent(depositionId, depositionEvent, "user@mail.com");

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Events[0].EventType == EventType.EndDeposition)), Times.Once());

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
            var expectedError = "Deposition not found";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.GetSharedDocument(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.SharingDocument)}.{nameof(Document.AddedBy)}" }))), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.SharingDocument)}.{nameof(Document.AddedBy)}" }))), Times.Once);
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
            var user = new User { Id = Guid.NewGuid() };
            var document = new Document { Id = Guid.NewGuid(), AddedById = user.Id, AddedBy = user };
            var deposition = new Deposition { Id = depositionId, SharingDocumentId = document.Id, SharingDocument = document };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.GetSharedDocument(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.SharingDocument)}.{nameof(Document.AddedBy)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(deposition.SharingDocument, result.Value);
            Assert.Equal(user, result.Value.AddedBy);
        }

        [Fact]
        public async Task CheckParticipant_ShouldReturnFail_ForNoDeposition()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            Deposition deposition = null;

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.CheckParticipant(depositionId, participantEmail);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task CheckParticipant_ShouldReturnIsUserAndParticipant_ForUserParticipant()
        {
            // Arrange
            var participantEmail = "participant@mail.com";

            var userEmail = "exisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail(participantEmail);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            // Act
            var result = await _depositionService.CheckParticipant(depositionId, participantEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Item2);
            Assert.NotNull(result.Value.Item1);
        }

        [Fact]
        public async Task CheckParticipant_ShouldReturnIsUserFalseAndParticipant_ForNoUserAndParticipant()
        {
            // Arrange
            var participantEmail = "participant@mail.com";

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail(participantEmail);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _depositionService.CheckParticipant(depositionId, participantEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(!result.Value.Item2);
            Assert.NotNull(result.Value.Item1);
        }

        [Fact]
        public async Task CheckParticipant_ShouldReturnIsUserTrueAndParticipantNull_ForUserNoParticipant()
        {
            // Arrange
            var participantEmail = "participant@mail.com";

            var userEmail = "exisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("foo@mail.com");

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            // Act
            var result = await _depositionService.CheckParticipant(depositionId, participantEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Item2);
            Assert.Null(result.Value.Item1);
        }

        [Fact]
        public async Task CheckParticipant_ShouldReturnIsUserFalseAndParticipantNull_ForNoUserAndNoParticipant()
        {
            // Arrange
            var participantEmail = "participant@mail.com";

            var userEmail = "exisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("foo@mail.com");

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _depositionService.CheckParticipant(depositionId, participantEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(!result.Value.Item2);
            Assert.Null(result.Value.Item1);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldSaveNewUserAndCallCognitoApi_ForNoUserAndNoParticipant()
        {
            // Arrange
            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("foo@mail.com");
            deposition.Id = depositionId;

            var participant = new Participant
            {
                User = user,
                UserId = user.Id,
                Email = guestEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Observer
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));
            _userServiceMock.Setup(x => x.AddGuestUser(It.IsAny<User>())).ReturnsAsync(Result.Ok(user));
            _userServiceMock.Setup(x => x.LoginGuestAsync(It.IsAny<string>())).ReturnsAsync(Result.Ok(new GuestToken()));

            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == guestEmail)), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldReturnAToken_ForARegisterUserAndParticipant()
        {
            // Arrange
            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail(guestEmail);
            deposition.Id = depositionId;

            var participant = new Participant
            {
                User = user,
                UserId = user.Id,
                Email = guestEmail,
                DepositionId = depositionId
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _userServiceMock.Setup(x => x.AddGuestUser(It.IsAny<User>())).ReturnsAsync(Result.Ok(user));
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>())).ReturnsAsync(participant);
            _userServiceMock.Setup(x => x.LoginGuestAsync(It.IsAny<string>())).ReturnsAsync(Result.Ok(new GuestToken()));

            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Never);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == guestEmail)), Times.Never);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldSaveNewUserAndCallCognitoApi_ForNoUserAndParticipant()
        {
            // Arrange
            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };
            var name = "Test";

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("participant@mail.com", false);
            deposition.Id = depositionId;

            var participant = new Participant
            {
                Name = name,
                Email = guestEmail,
                Role = ParticipantType.Observer,
                DepositionId = depositionId
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));
            _userServiceMock.Setup(x => x.AddGuestUser(It.IsAny<User>())).ReturnsAsync(Result.Ok(user));
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>())).ReturnsAsync(participant);
            _userServiceMock.Setup(x => x.LoginGuestAsync(It.IsAny<string>())).ReturnsAsync(Result.Ok(new GuestToken()));

            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant);

            // Assert
            _participantRepositoryMock.Verify(x => x.Update(It.Is<Participant>(x => x.Email == guestEmail)), Times.Once);
            Assert.True(result.IsSuccess);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == guestEmail)), Times.Once);
        }

        [Fact]
        public async Task AddParticipant_ShouldAddParticipantInDeposition_ForARegisteredUser()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = participantEmail };
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var participant = new Participant
            {
                Email = participantEmail,
                Role = ParticipantType.Observer
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            // Act
            var result = await _depositionService.AddParticipant(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == participantEmail)), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddParticipant_ShouldReturnFail_IfDepositionIsCompleted()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.Status = DepositionStatus.Completed;
            var expectedError = "The deposition is not longer available";
            var participant = new Participant
            {
                Email = participantEmail,
                Role = ParticipantType.Observer
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.AddParticipant(depositionId, participant);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Guid>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AddParticipant_ShouldReturnFail_IfDepositionIsCanceled()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.Status = DepositionStatus.Canceled;
            var expectedError = "The deposition is not longer available";
            var participant = new Participant
            {
                Email = participantEmail,
                Role = ParticipantType.Observer
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.AddParticipant(depositionId, participant);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Guid>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AddParticipant_ShouldReturnFail_IfParticipantIsWitnessAndDepositionHasWitness()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = participantEmail };
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.Participants.Single(x => x.Role == ParticipantType.Witness).UserId = Guid.NewGuid();
            var expectedError = "The deposition already has a participant as witness";
            var participant = new Participant
            {
                Email = participantEmail,
                Role = ParticipantType.Witness
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            // Act
            var result = await _depositionService.AddParticipant(depositionId, participant);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Guid>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetDepositionVideoInformation_ShouldReturnFail_IfDepositionDoesNotExist()
        {
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionService.GetDepositionVideoInformation(depositionId);

            //Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetDepositionVideoInformation_ShouldReturnFail_IfDepositionDoesNotHaveComposition()
        {

            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionService.GetDepositionVideoInformation(depositionId);

            //Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetDepositionVideoInformation_ShouldReturnAnEmptyUrl_IfDepositionCompositionIsNotCompleted()
        {

            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var composition = new Composition
            {
                Status = CompositionStatus.Progress
            };
            var events = new List<DepositionEvent>();
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(10), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(25), EventType = EventType.OffTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(50), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(58), EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(125), EventType = EventType.OffTheRecord });

            deposition.Events = events;
            deposition.Room.Composition = composition;
            deposition.Room.RecordingStartDate = DateTime.UtcNow;
            deposition.Room.EndDate = DateTime.UtcNow.AddSeconds(300);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionService.GetDepositionVideoInformation(depositionId);

            //Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.PublicUrl);
        }

        [Fact]
        public async Task GetDepositionVideoInformation_ShouldReturnOk_IfDepositionCompositionIsCompleted()
        {

            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var composition = new Composition
            {
                Status = CompositionStatus.Completed
            };
            var events = new List<DepositionEvent>();
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(10), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(25), EventType = EventType.OffTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(50), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(58), EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(125), EventType = EventType.OffTheRecord });

            deposition.Events = events;
            deposition.Room.Composition = composition;
            deposition.Room.RecordingStartDate = DateTime.UtcNow;
            deposition.Room.EndDate = DateTime.UtcNow.AddSeconds(300);

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), null)).Returns("urlMocked");

            //Act
            var result = await _depositionService.GetDepositionVideoInformation(depositionId);

            //Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.OffTheRecordTime == 210);
            Assert.True(result.Value.TotalTime == 300);
            Assert.True(result.Value.OnTheRecordTime == 90);
        }

        [Fact]
        public async Task GetDepositionCaption_ShouldReturnOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var user = new User { Id = Guid.NewGuid() };
            var document = new Document { Id = Guid.NewGuid() };
            var deposition = new Deposition { Id = depositionId, CaptionId = document.Id, Caption = document };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.GetDepositionCaption(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(deposition.Caption, result.Value);
        }

        [Fact]
        public async Task GetDepositionCaption_ShouldReturnFail_IfDepositionNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = "Deposition not found";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.GetDepositionCaption(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetDepositionCaption_ShouldReturnFail_IfThereIsNotCaption()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId };
            var expectedError = "Caption not found in this deposition";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.GetDepositionCaption(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetParticipantsList_ShouldReturnOk()
        {
            var depositionId = Guid.NewGuid();
            var participant = new Participant
            {
                DepositionId = depositionId
            };
            var lstParticipantList = new List<Participant>() { participant };
            _participantRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Participant, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(lstParticipantList);

            // Act
            var result = await _depositionService.GetDepositionParticipants(depositionId, ParticipantSortField.Role, SortDirection.Descend);

            // Assert
            _participantRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Participant, bool>>>(),
                It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<Participant>>>(result);
            Assert.True(result.Value.Any());
        }
        [Fact]
        public async Task GetParticipantsList_ShouldReturnOk_With_Zero_Participants()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var lstParticipantResult = new List<Participant>();
            //var expectedError = $"There are not participants for deposition with Id: {depositionId}";
            _participantRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Participant, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(lstParticipantResult);

            // Act
            var result = await _depositionService.GetDepositionParticipants(depositionId, ParticipantSortField.Role, SortDirection.Descend);

            // Assert
            _participantRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<Participant, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Participant, bool>>>(),
                It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<Participant>>>(result);
            Assert.True(result.IsSuccess);
            Assert.True(!result.Value.Any());
        }
        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldAddNewParticipant_WithExistingUser()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = participantEmail };
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant>() };
            var participant = new Participant
            {
                User = user,
                UserId = user.Id,
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Observer
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == participantEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Participant>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldAddNewParticipant_WithoutExistingUser()
        {
            // Arrange
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Participants = new List<Participant>() };
            var participant = new Participant
            {
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Observer
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == participantEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Participant>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldFail_IfDepositionNotFound()
        {
            // Arrange
            var expectedError = "Deposition not found";
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() {
                    new Participant() {
                        Email=participantEmail
                    }
                }
            };

            var participant = new Participant
            {
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Observer
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert 
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.IsType<Result<Participant>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldFail_IfParticipantAlreadyExists()
        {
            // Arrange
            var expectedError = "Participant already exists";
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() {
                    new Participant() {
                        Email=participantEmail
                    }
                }
            };

            var participant = new Participant
            {
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Observer
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert  
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.IsType<Result<Participant>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldFail_IfCourtReporterAlreadyExists()
        {
            // Arrange
            var expectedError = "The deposition already has a participant as court reporter";
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() {
                    new Participant() {
                        Role = ParticipantType.CourtReporter
                    }
                }
            };

            var participant = new Participant
            {
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.CourtReporter
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert  
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.IsType<Result<Participant>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ShouldFail_IfWitnessAlreadyExists()
        {
            // Arrange
            var expectedError = "The deposition already has a participant as witness";
            var participantEmail = "participant@mail.com";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                Participants = new List<Participant>() {
                    new Participant() {
                        Role = ParticipantType.Witness
                    }
                }
            };

            var participant = new Participant
            {
                Email = participantEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Witness
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.AddParticipantToExistingDeposition(depositionId, participant);

            // Assert  
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.IsType<Result<Participant>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveParticipant_ShouldReturnFail_IfDepositionNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var participantId = Guid.NewGuid();
            var expectedError = $"Deposition not found with ID {depositionId}";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
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

            // Act
            var result = await _depositionService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
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

            // Act
            var result = await _depositionService.RemoveParticipantFromDeposition(depositionId, participantId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}" }))), Times.Once);
            _permissionServiceMock.Verify(x => x.RemoveParticipantPermissions(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            _participantRepositoryMock.Verify(x => x.Remove(It.IsAny<Participant>()));
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }        

        [Fact]
        public async Task EditDepositionDetails_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid() };
            var expectedError = $"Deposition not found with ID {depositionMock.Id}";
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(new User { });
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, new FileTransferInfo(), false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))));
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task EditDepositionDetails_ShouldFail_DocumentUpload()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), CaseId = Guid.NewGuid() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Fail($"Error loading file {keyName}"));
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, fileMock, false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))));
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.Is<string>(p => p.Contains(depositionMock.CaseId.ToString())),
                It.IsAny<DocumentType>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task EditDepositionDetails_ShouldOk_ChangeFieldsAndCaption()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), CaseId = Guid.NewGuid(), Caption = new Document() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok(new Document()));
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, fileMock, true);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))));
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.Is<string>(p => p.Contains(depositionMock.CaseId.ToString())),
                It.IsAny<DocumentType>()), Times.Once);
            _documentServiceMock.Verify(dc => dc.DeleteUploadedFiles(It.IsAny<List<Document>>()), Times.AtLeastOnce);
            _depositionRepositoryMock.Verify(d => d.Update(It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EditDepositionDetails_ShouldOk_ChangeFieldsAndDeleteCaption()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), CaseId = Guid.NewGuid(), Caption = new Document() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, null, true);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))));
            _documentServiceMock.Verify(dc => dc.DeleteUploadedFiles(It.IsAny<List<Document>>()), Times.AtLeastOnce);
            _depositionRepositoryMock.Verify(d => d.Update(It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EditDepositionDetails_ShouldOk_ChangeFieldsWithOutDeleteCaption()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), CaseId = Guid.NewGuid(), Caption = new Document() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, null, false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.Caption)}" }))));
            _depositionRepositoryMock.Verify(d => d.Update(It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }
    }
}
