using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Twilio.Rest.Video.V1;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DepositionServiceTests : IDisposable
    {
        // It.IsAny<Participant>(): we need to refactor this file to have the test setup on the constructor
        private const RoomResource roomMock = null;
        private readonly DepositionService _depositionService;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IParticipantRepository> _participantRepositoryMock;
        private readonly Mock<IDepositionEventRepository> _depositionEventRepositoryMock;
        private readonly Mock<ISystemSettingsRepository> _systemSettingsRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<IBreakRoomService> _breakRoomServiceMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;
        private readonly DocumentConfiguration _documentConfiguration;
        private readonly DepositionConfiguration _depositionconfiguration;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly Mock<IAwsStorageService> _awsStorageServiceMock;
        private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;
        private readonly Mock<IOptions<DocumentConfiguration>> _depositionDocumentConfigurationMock;
        private readonly Mock<IOptions<DepositionConfiguration>> _depositionConfigurationMock;
        private readonly Mock<IOptions<UrlPathConfiguration>> _urlPathConfigurationMock;
        private readonly Mock<IOptions<EmailConfiguration>> _emailConfigurationMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IDocumentService> _documentServiceMock;
        private readonly Mock<ILogger<DepositionService>> _loggerMock;
        private readonly Mock<IMapper<Deposition, DepositionDto, CreateDepositionDto>> _depositionMapperMock;
        private readonly Mock<IMapper<Participant, ParticipantDto, CreateParticipantDto>> _participantMapperMock;
        private readonly Mock<IMapper<BreakRoom, BreakRoomDto, object>> _breakRoomMapperMock;
        private readonly Mock<ISignalRDepositionManager> _signalRNotificationManagerMock;
        private readonly Mock<IAwsEmailService> _awsEmailServiceMock;
        private readonly Mock<IActivityHistoryService> _activityHistoryServiceMock;
        private readonly Mock<IDepositionEmailService> _depositionEmailServiceMock;
        private readonly Mock<IOptions<CognitoConfiguration>> _cognitoConfigurationMock;
        private readonly CognitoConfiguration _cognitoConfiguration;
        private readonly EmailTemplateNames _emailTemplateNames;
        private readonly Mock<IOptions<EmailTemplateNames>> _emailTemplateNamesMock;

        private readonly List<Deposition> _depositions = new List<Deposition>();

        public DepositionServiceTests()
        {
            // Setup
            _depositionEventRepositoryMock = new Mock<IDepositionEventRepository>();

            _systemSettingsRepositoryMock = new Mock<ISystemSettingsRepository>();

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
            _depositionconfiguration = new DepositionConfiguration { CancelAllowedOffsetSeconds = "60", MinimumReScheduleSeconds = "300" };
            _depositionConfigurationMock = new Mock<IOptions<DepositionConfiguration>>();
            _depositionConfigurationMock.Setup(x => x.Value).Returns(_depositionconfiguration);

            _emailConfiguration = new EmailConfiguration { EmailNotification = "notifications@remotelegal.com", DepositionLink = "", LogoImageName = "", ImagesUrl = "" };
            _emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();
            _emailConfigurationMock.Setup(x => x.Value).Returns(_emailConfiguration);

            _urlPathConfiguration = new UrlPathConfiguration { FrontendBaseUrl = "" };
            _urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            _urlPathConfigurationMock.Setup(x => x.Value).Returns(_urlPathConfiguration);

            _cognitoConfiguration = new CognitoConfiguration { AWSRegion = "test-region" };
            _cognitoConfigurationMock = new Mock<IOptions<CognitoConfiguration>>();
            _cognitoConfigurationMock.Setup(x => x.Value).Returns(_cognitoConfiguration);

            _participantMapperMock = new Mock<IMapper<Participant, ParticipantDto, CreateParticipantDto>>();
            _depositionMapperMock = new Mock<IMapper<Deposition, DepositionDto, CreateDepositionDto>>();
            _breakRoomMapperMock = new Mock<IMapper<BreakRoom, BreakRoomDto, object>>();

            _signalRNotificationManagerMock = new Mock<ISignalRDepositionManager>();

            _awsEmailServiceMock = new Mock<IAwsEmailService>();
            _activityHistoryServiceMock = new Mock<IActivityHistoryService>();
            _depositionEmailServiceMock = new Mock<IDepositionEmailService>();

            _emailTemplateNames = new EmailTemplateNames { DownloadAssetsEmail = "TestEmailTemplate", DownloadCertifiedTranscriptEmail = "TestEmailTemplate" };
            _emailTemplateNamesMock = new Mock<IOptions<EmailTemplateNames>>();
            _emailTemplateNamesMock.Setup(x => x.Value).Returns(_emailTemplateNames);

            _depositionService = new DepositionService(
                _depositionRepositoryMock.Object,
                _participantRepositoryMock.Object,
                _depositionEventRepositoryMock.Object,
                _systemSettingsRepositoryMock.Object,
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
                _depositionMapperMock.Object,
                _participantMapperMock.Object,
                _depositionConfigurationMock.Object,
                _signalRNotificationManagerMock.Object,
                _awsEmailServiceMock.Object,
                _urlPathConfigurationMock.Object,
                _emailConfigurationMock.Object,
                _breakRoomMapperMock.Object,
                _activityHistoryServiceMock.Object,
                _depositionEmailServiceMock.Object,
                _cognitoConfigurationMock.Object,
                _emailTemplateNamesMock.Object);

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
            var user = new User() { IsAdmin = true };
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            var depositionDocuments = DepositionFactory.GetDocumentList();
            var errorMessage = $"Requester with email {deposition.Requester.EmailAddress} not found";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error("Mocked error")));

            // Act
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, depositionDocuments, user);

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
            var user = new User() { IsAdmin = true };
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
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, null, user);

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
            var user = new User() { IsAdmin = true };
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
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, null, user);

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
            var user = new User() { IsAdmin = true };
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
            var result = await _depositionService.GenerateScheduledDeposition(caseId, deposition, documents, user);

            //Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(captionDocument, result.Value.Caption);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnAllDepositions_WhenStatusParameterIsNull()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());
            _depositions.AddRange(DepositionFactory.GetDepositionList());
            var upcomingList = _depositions.FindAll(x => x.Status == DepositionStatus.Pending);
            var depostionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsQueryable());
            _depositionRepositoryMock.Setup(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>())
               ).ReturnsAsync(depostionsResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(depostionsResult.Item2.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });
            var filter = new DepositionFilterDto
            {
                Page = 1,
                PageSize = 20
            };

            // Act
            var result = await _depositionService.GetDepositionsByStatus(filter);

            Assert.NotEmpty(result.Depositions);
            _depositionRepositoryMock.Verify(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()),
               Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionsDepositionsByStatus_ShouldReturnPendingDepositions_WhenStatusParameterIsPending()
        {
            _depositions.AddRange(DepositionFactory.GetDepositionList());
            var upcomingList = _depositions.FindAll(x => x.Status == DepositionStatus.Pending);
            var depositionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsQueryable());
            _depositionRepositoryMock.SetupSequence(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>())
               ).ReturnsAsync(depositionsResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(depositionsResult.Item2.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });
            var filter = new DepositionFilterDto
            {
                Status = DepositionStatus.Pending,
                Page = 1,
                PageSize = 20
            };

            // Act
            var result = await _depositionService.GetDepositionsByStatus(filter);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()),
               Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionsByStatus_ShouldReturnOrderedDepositionsListByRequester_WhenSortDirectionIsAscendAndSortedFieldIsRequester()
        {
            // Arrange
            var sortedList = DepositionFactory.GetDepositionsWithRequesters().OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName);
            _depositions.AddRange(sortedList);
            var upcomingList = _depositions.OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName).ToList();
            var depositionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsQueryable());

            _depositionRepositoryMock.Setup(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>())
               ).ReturnsAsync(depositionsResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(depositionsResult.Item2.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });
            var filter = new DepositionFilterDto
            {
                SortedField = DepositionSortField.Requester,
                SortDirection = SortDirection.Ascend,
                Page = 1,
                PageSize = 20
            };

            // Act
            var result = await _depositionService.GetDepositionsByStatus(filter);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()),
               Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
            Assert.True(result.Depositions.Any());
            Assert.True(result.Depositions.Count == 5);
            Assert.True(result.TotalUpcoming == 5);
            Assert.True(result.TotalPast == 2);
        }

        [Fact]
        public async Task GetDepositionsByStatus_ShouldOrderByThen_WhenSortedFieldIsRequester()
        {
            // Arrange
            var sortedList = DepositionFactory.GetDepositionsWithRequesters().OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName);
            _depositions.AddRange(sortedList);
            var upcomingList = _depositions.OrderBy(x => x.Requester.FirstName).ThenBy(x => x.Requester.LastName).ToList();
            var depositionsResult = new Tuple<int, IQueryable<Deposition>>(upcomingList.Count, upcomingList.AsQueryable());

            _depositionRepositoryMock.Setup(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>())
               ).ReturnsAsync(depositionsResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(depositionsResult.Item2.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });
            var filter = new DepositionFilterDto
            {
                SortedField = DepositionSortField.Requester,
                SortDirection = SortDirection.Ascend,
                Page = 1,
                PageSize = 20
            };

            // Act
            var result = await _depositionService.GetDepositionsByStatus(filter);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetByFilterPaginationQueryable(
               It.IsAny<Expression<Func<Deposition, bool>>>(),
               It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()),
               Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
            Assert.True(result.Depositions.Any());
            Assert.True(result.Depositions.Count == 5);
            Assert.True(result.TotalUpcoming == 5);
            Assert.True(result.TotalPast == 2);
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
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Room), nameof(Deposition.PreRoom), nameof(Deposition.Participants) }))), Times.Once());
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.NotNull(deposition.TimeZone);
            Assert.Equal("America/New_York", deposition.TimeZone);
        }

        [Fact]
        public async Task JoinDeposition_ShouldReturnError_WhenDepositionIsCompleted()
        {
            // Arrange
            var userEmail = "testing@mail.com";
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            deposition.Status = DepositionStatus.Completed;
            var errorMessage = $"Deposition with id {depositionId} has already ended.";

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once());
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.IsAny<Guid>(), It.Is<string[]>(a => a.SequenceEqual(new[] { nameof(Deposition.Room), nameof(Deposition.PreRoom), nameof(Deposition.Participants) }))), Times.Once());
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.True(result.IsFailed);
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
            var courtReporterUser = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter@email.com", FirstName = "userFirstName", LastName = "userLastName" };
            var currentParticipant = new Participant { Name = "ParticipantName", Role = ParticipantType.Observer, User = user, IsAdmitted = true };
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
                   new Participant { Name = "ParticipantName", Role = ParticipantType.CourtReporter, User = courtReporterUser, HasJoined = true },
                   currentParticipant
                },
                TimeZone = "AT",
                IsOnTheRecord = true,
                Job = "Test123"
            };
            var roomList = new List<RoomResource> { roomMock };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(), It.IsAny<RoomResource.RoomStatusEnum>())).ReturnsAsync(roomList);

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room), It.IsAny<bool>()), Times.Never);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == currentParticipant.Role),
                It.Is<string>(a => a == userEmail), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.TimeZone, result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }

        [Fact]
        public async Task JoinDeposition_ShouldStartRoom_WhenFirstCourtReporterJoins()
        {
            // Arrange
            var userEmail = "courtReporter@email.com";
            var courtReporterUser = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
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
                   new Participant { Email = userEmail, Role = ParticipantType.CourtReporter, User = courtReporterUser }
                },
                TimeZone = "TetingTimeZone",
                IsOnTheRecord = true,
                Job = "Test123"
            };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(courtReporterUser));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room), It.IsAny<bool>()), Times.Once);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == courtReporterUser),
                It.Is<ParticipantType>(a => a == ParticipantType.CourtReporter),
                It.Is<string>(a => a == userEmail), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            _signalRNotificationManagerMock.Verify(s => s.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Once());
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }

        [Fact]
        public async Task JoinDeposition_ShouldJoinAsWitness_WhenParticipantIsWitness_AndCourtReporterHasJoined()
        {
            // Arrange
            var userEmail = "witness@email.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
            var courtReporterUser = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter@email.com", FirstName = "userFirstName", LastName = "userLastName" };
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
                   new Participant { Name = "ParticipantName", Role = ParticipantType.CourtReporter, User = courtReporterUser, HasJoined = true },
                   new Participant { Email = userEmail, Role = ParticipantType.Witness, User = user, IsAdmitted = true }
                },
                TimeZone = "America/Puerto_Rico",
                IsOnTheRecord = true,
                Job = "Test123"
            };

            var roomList = new List<RoomResource> { roomMock };

            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(), It.IsAny<RoomResource.RoomStatusEnum>())).ReturnsAsync(roomList);

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room), It.IsAny<bool>()), Times.Never);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == ParticipantType.Witness),
                It.Is<string>(a => a == userEmail), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }

        [Fact]
        public async Task JoinDeposition_ShouldJoinDepoAsCourtReporter_WhenParticipantIsCourtReporter_AndAnotherCourtReporterHasJoined()
        {
            // Arrange
            var courtReporter1 = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter1@email.com", FirstName = "userFirstName", LastName = "userLastName" };
            var courtReporter2 = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter2@email.com", FirstName = "userFirstName", LastName = "userLastName" };
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
                   new Participant { Name = "ParticipantCRJoined", Role = ParticipantType.CourtReporter, User = courtReporter1, HasJoined = true },
                   new Participant { Name = "ParticipantCR", Role = ParticipantType.CourtReporter, User = courtReporter2, IsAdmitted = true, HasJoined = false }
                },
                TimeZone = "America/Puerto_Rico",
                IsOnTheRecord = true,
                Job = "Test123"
            };
            var roomList = new List<RoomResource> { roomMock };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(courtReporter2));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));
            _roomServiceMock.Setup(x => x.GetTwilioRoomByNameAndStatus(It.IsAny<string>(), It.IsAny<RoomResource.RoomStatusEnum>())).ReturnsAsync(roomList);

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, "courtreporter2@email.com");

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.Room), It.IsAny<bool>()), Times.Never);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.Room.Name),
                It.Is<User>(a => a == courtReporter2),
                It.Is<ParticipantType>(a => a == ParticipantType.CourtReporter),
                It.Is<string>(a => a == "courtreporter2@email.com"), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }

        [Fact]
        public async Task JoinDeposition_ShouldJoinAndStartPreDepoAsWitness_WhenParticipantIsWitnessAndPreDepoIsNotInProgress_AndCourtReporterHasNotJoined()
        {
            // Arrange
            var userEmail = "witness@email.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
            var courtReporterUser = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter@email.com", FirstName = "userFirstName", LastName = "userLastName" };
            var token = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = new Deposition
            {
                PreRoom = new Room
                {
                    Id = Guid.NewGuid(),
                    Status = RoomStatus.Created,
                    Name = "TestingRoom"
                },
                Participants = new List<Participant>
                {
                   new Participant { Name = "ParticipantName", Role = ParticipantType.Observer },
                   new Participant { Name = "ParticipantName", Role = ParticipantType.CourtReporter, User = courtReporterUser, HasJoined = false },
                   new Participant { Email = userEmail, Role = ParticipantType.Witness, User = user, IsAdmitted = true }
                },
                TimeZone = "America/Puerto_Rico",
                IsOnTheRecord = true,
                Job = "Test123"
            };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.PreRoom), It.IsAny<bool>()), Times.Once);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.PreRoom.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == ParticipantType.Witness),
                It.Is<string>(a => a == userEmail), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }

        [Fact]
        public async Task JoinDeposition_ShouldJoinPreDepoAsWitness_WhenParticipantIsWitnessAndPreDepoIsInProgress_AndCourtReporterHasNotJoined()
        {
            // Arrange
            var userEmail = "witness@email.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail, FirstName = "userFirstName", LastName = "userLastName" };
            var courtReporterUser = new User { Id = Guid.NewGuid(), EmailAddress = "courtreporter@email.com", FirstName = "userFirstName", LastName = "userLastName" };
            var token = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = new Deposition
            {
                PreRoom = new Room
                {
                    Id = Guid.NewGuid(),
                    Status = RoomStatus.InProgress,
                    Name = "TestingRoom"
                },
                Participants = new List<Participant>
                {
                   new Participant { Name = "ParticipantName", Role = ParticipantType.Observer },
                   new Participant { Name = "ParticipantName", Role = ParticipantType.CourtReporter, User = courtReporterUser, HasJoined = false },
                   new Participant { Email = userEmail, Role = ParticipantType.Witness, User = user, IsAdmitted = true }
                },
                TimeZone = "America/Puerto_Rico",
                IsOnTheRecord = true,
                Job = "Test123"
            };
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());
            _roomServiceMock.Setup(x => x.GenerateRoomToken(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<ParticipantType>(), It.IsAny<string>(), It.IsAny<Participant>(), It.IsAny<ChatDto>())).ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _depositionService.JoinDeposition(depositionId, userEmail);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once());
            _roomServiceMock.Verify(x => x.StartRoom(It.Is<Room>(a => a == deposition.PreRoom), It.IsAny<bool>()), Times.Never);
            _roomServiceMock.Verify(x => x.GenerateRoomToken(
                It.Is<string>(a => a == deposition.PreRoom.Name),
                It.Is<User>(a => a == user),
                It.Is<ParticipantType>(a => a == ParticipantType.Witness),
                It.Is<string>(a => a == userEmail), It.IsAny<Participant>(), It.IsAny<ChatDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<JoinDepositionDto>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TimeZone);
            Assert.Equal(deposition.IsOnTheRecord, result.Value.IsOnTheRecord);
            Assert.Equal(token, result.Value.Token);
            Assert.Equal(deposition.Job, result.Value.JobNumber);
        }


        [Fact]
        public async Task JoinBreakRoom_ShouldReturnError_WhenDepositionDoesNotExist()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
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

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldReturnError_WhenBreakRoomIsDisabled()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "disabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);

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
            var token = "token";
            var user = new User()
            {
                Id = Guid.NewGuid(),
                EmailAddress = "tesmail@test.com"
            };
            var deposition = new Deposition
            {
                Id = Guid.NewGuid(),
                IsOnTheRecord = false,
                Participants = new List<Participant>()
                {
                    new Participant()
                    {
                        User = user,
                        UserId = user.Id
                    }
                }
            };

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _breakRoomServiceMock.Setup(x => x.JoinBreakRoom(breakRoomId, It.IsAny<Participant>())).ReturnsAsync(Result.Ok(token));
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value, token);
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldFailIfParticipantDoesntExist()
        {
            // Arrange
            var expectedError = "User is neither a Participant for this Deposition nor an Admin";
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var deposition = new Deposition()
            {
                Participants = new List<Participant>()
            };

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldFailIfBreakRoomLocked()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var breakRoom = new BreakRoom
            {
                Name = "Test BreakRoom",
                IsLocked = true,
            };
            var user = new User
            {
                Id = Guid.NewGuid()
            };
            var expectedError = $"The Break Room[{ breakRoom.Name}] is currently locked.";
            var deposition = new Deposition()
            {
                Participants = new List<Participant>
                {
                    new Participant
                    {
                        User = user,
                        UserId = user.Id
                    }
                },
            };

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _breakRoomServiceMock.Setup(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>())).ReturnsAsync(Result.Fail(new InvalidInputError(expectedError)));
            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionId), It.IsAny<string[]>()), Times.Once);
            _breakRoomServiceMock.Verify(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldOkIfBreakRoomLocked_CourtReporterUser()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var breakRoom = new BreakRoom
            {
                Name = "Test BreakRoom",
                IsLocked = true,
            };
            var user = new User
            {
                Id = Guid.NewGuid()
            };
            var expectedError = $"The Break Room[{ breakRoom.Name}] is currently locked.";
            var deposition = new Deposition()
            {
                Participants = new List<Participant>
                {
                    new Participant
                    {
                        User = user,
                        UserId = user.Id,
                        Role = ParticipantType.CourtReporter
                    }
                },
            };

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _breakRoomServiceMock.Setup(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>())).ReturnsAsync(Result.Fail(new InvalidInputError(expectedError)));
            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionId), It.IsAny<string[]>()), Times.Once);
            _breakRoomServiceMock.Verify(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task JoinBreakRoom_ShouldOkIfBreakRoomUnlocked_AnyUser()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();

            var breakRoom = new BreakRoom
            {
                Name = "Test BreakRoom",
                IsLocked = false,
            };
            var user = new User
            {
                Id = Guid.NewGuid()
            };
            var expectedError = $"The Break Room[{ breakRoom.Name}] is currently locked.";
            var deposition = new Deposition()
            {
                Participants = new List<Participant>
                {
                    new Participant
                    {
                        User = user,
                        UserId = user.Id,
                        Role = ParticipantType.Observer
                    }
                },
            };

            var systemSetting = new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "enabled", Id = Guid.Parse("dd502081-525a-4faa-a9aa-3de98e4368e7"), CreationDate = DateTime.UtcNow };

            _systemSettingsRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(systemSetting);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _breakRoomServiceMock.Setup(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>())).ReturnsAsync(Result.Fail(new InvalidInputError(expectedError)));
            // Act
            var result = await _depositionService.JoinBreakRoom(depositionId, breakRoomId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionId), It.IsAny<string[]>()), Times.Once);
            _breakRoomServiceMock.Verify(x => x.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
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
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _depositionService.EndDeposition(depositionId);

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
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _roomServiceMock.Setup(x => x.EndRoom(It.IsAny<Room>(), It.IsAny<string>())).ReturnsAsync(() => Result.Ok(new Room()));
            _backgroundTaskQueueMock.Setup(x => x.QueueBackgroundWorkItem(It.IsAny<BackgroundTaskDto>()));
            var userMock = Mock.Of<User>();
            userMock.EmailAddress = deposition.Participants.First().Email;
            var users = new List<User>() { 
                userMock
            };
            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(users);
            _permissionServiceMock.Setup(x => x.SetCompletedDepositionPermissions(It.IsAny<Participant>(), It.IsAny<Guid>()));

            // Act
            var result = await _depositionService.EndDeposition(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.AtLeast(1));
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Status == DepositionStatus.Completed && d.CompleteDate.HasValue)), Times.AtLeast(1));
            _roomServiceMock.Verify(mock => mock.EndRoom(It.IsAny<Room>(), It.IsAny<string>()), Times.Once());
            _permissionServiceMock.Verify(x => x.SetCompletedDepositionPermissions(It.IsAny<Participant>(), It.IsAny<Guid>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EndDeposition_ShouldReturnFail_WhenPermissionServiceFail()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var identity = Guid.NewGuid().ToString();
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            _depositions.Add(deposition);
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(() => _depositions.FirstOrDefault());

            _roomServiceMock.Setup(x => x.EndRoom(It.IsAny<Room>(), It.IsAny<string>())).ReturnsAsync(() => Result.Ok(new Room()));
            _backgroundTaskQueueMock.Setup(x => x.QueueBackgroundWorkItem(It.IsAny<BackgroundTaskDto>()));
            var userMock = Mock.Of<User>();
            userMock.EmailAddress = deposition.Participants.First().Email;
            var users = new List<User>() {
                userMock
            };
            _userServiceMock.Setup(x => x.GetUsersByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(users);
            _permissionServiceMock.Setup(x => x.SetCompletedDepositionPermissions(It.IsAny<Participant>(), It.IsAny<Guid>())).Throws(new Exception());

            // Act
            var result = await _depositionService.EndDeposition(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.AtLeast(1));
            _depositionRepositoryMock.Verify(mock => mock.Update(It.Is<Deposition>(d => d.Status == DepositionStatus.Completed && d.CompleteDate.HasValue)), Times.AtLeast(1));
            _roomServiceMock.Verify(mock => mock.EndRoom(It.IsAny<Room>(), It.IsAny<string>()), Times.Once());
            _permissionServiceMock.Verify(x => x.SetCompletedDepositionPermissions(It.IsAny<Participant>(), It.IsAny<Guid>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
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
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("foo@mail.com");
            deposition.Id = depositionId;
            deposition.Participants.Add(new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Witness,
                IsAdmitted = true
            });

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
            _activityHistoryServiceMock.Setup(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()));
            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant, activity);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _activityHistoryServiceMock.Verify(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldSaveNewUserAndCallCognitoApi_ForNoUserAndNoParticipantAsWitness()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, new Guid());

            var witnessParticipant = deposition.Participants.FirstOrDefault(w => w.Role == ParticipantType.Witness);
            witnessParticipant.Email = string.Empty;
            witnessParticipant.IsAdmitted = true;

            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };
            var participant = new Participant
            {
                User = user,
                UserId = user.Id,
                Email = guestEmail,
                DepositionId = depositionId,
                Role = ParticipantType.Witness
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));
            _userServiceMock.Setup(x => x.AddGuestUser(It.IsAny<User>())).ReturnsAsync(Result.Ok(user));
            _userServiceMock.Setup(x => x.LoginGuestAsync(It.IsAny<string>())).ReturnsAsync(Result.Ok(new GuestToken()));
            _activityHistoryServiceMock.Setup(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()));
            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant, activity);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Once);
            _activityHistoryServiceMock.Verify(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldReturnAToken_ForARegisterUserAndParticipant()
        {
            // Arrange
            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail(guestEmail);
            deposition.Id = depositionId;
            deposition.Participants.Add(new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Witness,
                IsAdmitted = true
            });

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
            _activityHistoryServiceMock.Setup(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()));
            _permissionServiceMock.Setup(x => x.AddParticipantPermissions(It.IsAny<Participant>()));

            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant, activity);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == depositionId)), Times.Never);
            _permissionServiceMock.Verify(x => x.AddParticipantPermissions(It.Is<Participant>(x => x.Email == guestEmail)), Times.Once);
            _activityHistoryServiceMock.Verify(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task JoinGuestParticipant_ShouldSaveNewUserAndCallCognitoApi_ForNoUserAndParticipant()
        {
            // Arrange
            var guestEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = guestEmail };
            var name = "Test";
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };

            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("participant@mail.com", false);
            deposition.Id = depositionId;
            deposition.Participants.Add(new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Witness,
                IsAdmitted = true
            });

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
            _activityHistoryServiceMock.Setup(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()));

            // Act
            var result = await _depositionService.JoinGuestParticipant(depositionId, participant, activity);

            // Assert
            _participantRepositoryMock.Verify(x => x.Update(It.Is<Participant>(x => x.Email == guestEmail)), Times.Once);
            Assert.True(result.IsSuccess);
            _activityHistoryServiceMock.Verify(x => x.AddActivity(It.IsAny<ActivityHistory>(), It.IsAny<User>(), It.IsAny<Deposition>()), Times.Once);
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
        public async Task AddParticipant_ShouldAddParticipantInDeposition_ForTechExpert()
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
                Role = ParticipantType.TechExpert
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
        public async Task AddParticipant_ShouldAddParticipantInDeposition_ForAdminAsWitness()
        {
            // Arrange
            var participantEmail = "admin@mail.com";
            var adminUser = UserFactory.GetGuestUserByGivenIdAndEmail(Guid.NewGuid(), participantEmail);
            adminUser.IsAdmin = true;
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("observer@participant.com");
            var participant = new Participant
            {
                Email = participantEmail,
                Role = ParticipantType.Witness,
                User = adminUser
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(adminUser));

            // Act
            var result = await _depositionService.AddParticipant(depositionId, participant);

            // Assert
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(x => x.Id == deposition.Id)), Times.Once);
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
            var participantEmail = "participant@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = participantEmail, FirstName = "Anne", LastName = "Roxy" };
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
            deposition.Case = new Case { Name = "Case123" };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition); _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _awsStorageServiceMock.Setup(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), null, false)).Returns("urlMocked");

            //Act
            var result = await _depositionService.GetDepositionVideoInformation(depositionId);

            //Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.TotalTime == 290);
            Assert.True(result.Value.OffTheRecordTime == 200);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
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
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.IsType<Result<Participant>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UpdateParticipantOnExistingDepositions_ShouldUpdateParticipantPermissions_WhenANewUserIsGiven()
        {
            //Arrange
            var email = "test@user.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail(email, false);
            var depositions = new List<Deposition> { deposition };
            _depositionRepositoryMock.Setup(s => s.GetByFilter(d => d.Participants.Any(u =>
                                                    u.Email == user.EmailAddress), new[] { nameof(Deposition.Participants) }))
                                    .ReturnsAsync(depositions);

            _permissionServiceMock.Setup(s => s.AddParticipantPermissions(It.IsAny<Participant>()));

            //Act
            var result = await _depositionService.UpdateParticipantOnExistingDepositions(user);

            //Assert
            _depositionRepositoryMock.Verify(s => s.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _depositionRepositoryMock.Verify(s => s.Update(It.IsAny<Deposition>()), Times.Once);
            _permissionServiceMock.Verify(s => s.AddParticipantPermissions(It.IsAny<Participant>()), Times.Once);
            Assert.IsType<Result<List<Deposition>>>(result);
            var updatedParticipants = result.Value.FirstOrDefault(d => d.Participants.Any(p => p.Email == email)).Participants;
            Assert.True(!updatedParticipants.Any(p => p.UserId != user.Id));
            Assert.True(!updatedParticipants.Any(p => p.User == null));
        }

        [Fact]
        public async Task UpdateParticipantOnExistingDepositions_ShouldReturnOkWithAnEmptyListOfDepositions_WhenNoDepositionsAreFound()
        {
            //Arrange
            var email = "test@user.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            _depositionRepositoryMock.Setup(s => s.GetByFilter(d => d.Participants.Any(u =>
                                                    u.Email == user.EmailAddress), new[] { nameof(Deposition.Participants) }))
                                    .ReturnsAsync(new List<Deposition>());

            //Act
            var result = await _depositionService.UpdateParticipantOnExistingDepositions(user);

            //Assert
            _depositionRepositoryMock.Verify(s => s.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _depositionRepositoryMock.Verify(s => s.Update(It.IsAny<Deposition>()), Times.Never);
            _permissionServiceMock.Verify(s => s.AddParticipantPermissions(It.IsAny<Participant>()), Times.Never);
            Assert.IsType<Result<List<Deposition>>>(result);
            Assert.True(result.Value.Count == 0);
        }

        [Fact]
        public async Task EditDepositionDetails_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid() };
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(new User { });
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, new FileTransferInfo(), false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Never);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()));
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
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
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()));
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
            var depositionMock = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            depositionMock.Room = RoomFactory.GetRoomById(Guid.NewGuid());
            var testEmail = "test@test.com";
            var fileName = "testFile.pdf";
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
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()));
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
            var depositionMock = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            depositionMock.Room = RoomFactory.GetRoomById(Guid.NewGuid());
            var testEmail = "test@test.com";
            var userMock = new User() { EmailAddress = testEmail };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, null, true);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()));
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
            var depositionMock = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            depositionMock.Room = RoomFactory.GetRoomById(Guid.NewGuid());
            var testEmail = "test@test.com";
            var userMock = new User() { EmailAddress = testEmail };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(depositionMock);
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.EditDepositionDetails(depositionMock, null, false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()));
            _depositionRepositoryMock.Verify(d => d.Update(It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetDepositionsByFilter_ShouldFail_InvalidRangeOfDates()
        {
            //Arrange
            var expectedError = "Invalid range of dates";
            var filter = new DepositionFilterDto
            {
                MinDate = DateTime.Now.AddDays(5),
                MaxDate = DateTime.Now,
            };

            //Act
            var result = await _depositionService.GetDepositionsByFilter(filter);

            //Assert
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetDepositionsByFilter_ShouldReturnUpcomingDepositions_ForEmptyFilters()
        {
            var filter = new DepositionFilterDto
            {
                PageSize = 20
            };

            var upcommingDepositions = new List<Deposition> {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now.AddHours(30),
                    EndDate = DateTime.Now.AddHours(35),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now.AddHours(60),
                    EndDate = DateTime.Now.AddHours(65),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow
                }
            };

            var depositionResult = new Tuple<int, IQueryable<Deposition>>(upcommingDepositions.Count, upcommingDepositions.AsQueryable());

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            _depositionRepositoryMock.Setup(x => x.GetByFilterPaginationQueryable(
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
                It.IsAny<string[]>(),
                It.IsAny<int>(),
                It.IsAny<int>())
                ).ReturnsAsync(depositionResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(upcommingDepositions.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);

            var result = await _depositionService.GetDepositionsByFilter(filter);

            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.TotalUpcoming == 2);
            Assert.True(result.Value.TotalPast == 2);
            Assert.True(result.Value.Depositions.Count == 2);
        }

        [Fact]
        public async Task GetDepositionsByFilter_ShouldReturnPassedDepositions_ForPastDepositionFilter()
        {
            var filter = new DepositionFilterDto
            {
                PastDepositions = true,
                PageSize = 20
            };

            var upcommingDepositions = new List<Deposition> {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now.AddHours(30),
                    EndDate = DateTime.Now.AddHours(35),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now.AddHours(60),
                    EndDate = DateTime.Now.AddHours(65),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow
                }
            };

            var depositionResult = new Tuple<int, IQueryable<Deposition>>(upcommingDepositions.Count, upcommingDepositions.AsQueryable());
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            _depositionRepositoryMock.Setup(x => x.GetByFilterPaginationQueryable(
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
                It.IsAny<string[]>(),
                It.IsAny<int>(),
                It.IsAny<int>())
                ).ReturnsAsync(depositionResult);
            _depositionRepositoryMock.Setup(x => x.GetDepositionWithAdmittedParticipant(It.IsAny<IQueryable<Deposition>>())).ReturnsAsync(upcommingDepositions.ToList());
            _depositionRepositoryMock.Setup(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>())).ReturnsAsync(2);

            var result = await _depositionService.GetDepositionsByFilter(filter);

            Assert.True(result.IsSuccess);
            _depositionRepositoryMock.Verify(x => x.GetByFilterPaginationQueryable(
                It.IsAny<Expression<Func<Deposition, bool>>>(),
                It.IsAny<Func<IQueryable<Deposition>, IOrderedQueryable<Deposition>>>(),
                It.IsAny<string[]>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
                Times.Once
                );
            _depositionRepositoryMock.Verify(x => x.GetCountByFilter(It.IsAny<Expression<Func<Deposition, bool>>>()), Times.Once);
            Assert.True(result.Value.TotalPast == 2);
            Assert.True(result.Value.TotalUpcoming == 2);
            Assert.True(result.Value.Depositions.Count == 2);
        }

        [Fact]
        public async Task CancelDeposition_ShouldReturnFail_WhenDepositionIsCloseToStart()
        {
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.StartDate = DateTime.UtcNow.AddSeconds(59);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            var result = await _depositionService.CancelDeposition(depositionId);

            Assert.True(result.IsFailed);
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task CancelDeposition_ShouldReturnOk_WhenDepositionIsNotCloseToStart()
        {
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.StartDate = DateTime.UtcNow.AddMinutes(2);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            var result = await _depositionService.CancelDeposition(depositionId);

            Assert.True(result.IsSuccess);
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task RevertCancel_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid() };
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(new User { });
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionService.RevertCancel(depositionMock, new FileTransferInfo(), false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Never);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { nameof(Deposition.Caption), nameof(Deposition.Case), nameof(Deposition.Participants), nameof(Deposition.Room) }))));
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task RevertCancel_ShouldFail_DocumentUploadFail()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), CaseId = Guid.NewGuid(), Room = RoomFactory.GetRoomById(Guid.NewGuid())};
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
            var result = await _depositionService.RevertCancel(depositionMock, fileMock, false);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { nameof(Deposition.Caption), nameof(Deposition.Case), nameof(Deposition.Participants), nameof(Deposition.Room) }))));
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RevertCancel_ShouldOk_ChangeStatusAndOtherFields()
        {
            //Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), Status = DepositionStatus.Pending, CaseId = Guid.NewGuid(), Caption = new Document()};
            var currentDepositionMock = new Deposition() { Id = Guid.NewGuid(), Status = DepositionStatus.Canceled, CaseId = Guid.NewGuid(), Caption = new Document(), Room = RoomFactory.GetRoomById(Guid.NewGuid()) };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };

            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(currentDepositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok(new Document()));
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);
            //Act
            var result = await _depositionService.RevertCancel(depositionMock, fileMock, true);

            //Assert
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.Is<string[]>(i => i.SequenceEqual(new[] { nameof(Deposition.Caption), nameof(Deposition.Case), nameof(Deposition.Participants), nameof(Deposition.Room) }))));
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Once);
            _documentServiceMock.Verify(dc => dc.DeleteUploadedFiles(It.IsAny<List<Document>>()), Times.AtLeastOnce);
            _depositionRepositoryMock.Verify(d => d.Update(It.IsAny<Deposition>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.Value.Status == currentDepositionMock.Status);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AdmitDenyParticipant_ShouldFail_ParticipantNoFound()
        {
            //Arrange
            var participantId = Guid.NewGuid();
            var expectedError = $"Participant not found with Id: {participantId}";
            _participantRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Participant)null);

            //Act
            var result = await _depositionService.AdmitDenyParticipant(participantId, true);

            //Assert
            _participantRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(x => x == participantId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task AdmitDenyParticipant_ShouldOk()
        {
            //Arrange
            var participantId = Guid.NewGuid();
            var participant = new Participant()
            {
                Id = participantId,
                Email = "test@testemail.com",
                DepositionId = Guid.NewGuid()
            };
            _participantRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(participant);

            //Act
            var result = await _depositionService.AdmitDenyParticipant(participantId, true);

            //Assert
            _participantRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(x => x == participantId), It.IsAny<string[]>()), Times.Once);
            _signalRNotificationManagerMock.Verify(x => x.SendDirectMessage(It.Is<string>(t => t == participant.Email), It.IsAny<NotificationDto>()), Times.Once);
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToDepositionAdmins(It.Is<Guid>(t => t == participant.DepositionId), It.IsAny<NotificationDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReScheduleDeposition_ShouldReturnFail_IfStarDateIsLowerThanMinimum()
        {
            // Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), StartDate = DateTime.UtcNow.AddSeconds(259), EndDate = DateTime.UtcNow.AddSeconds(600), Status = DepositionStatus.Pending, CaseId = Guid.NewGuid(), Caption = new Document() };
            var currentDepositionMock = new Deposition() { Id = Guid.NewGuid(), Status = DepositionStatus.Canceled, CaseId = Guid.NewGuid(), Caption = new Document() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var expectedError = $"Unable to edit the deposition";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(currentDepositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok(new Document()));
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);

            // Act
            var result = await _depositionService.ReScheduleDeposition(depositionMock, fileMock, true);

            // Assert
            Assert.True(result.IsFailed);
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Never);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()), Times.Never);
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Never);
        }

        [Fact]
        public async Task ReScheduleDeposition_ShouldReturnFail_IfStarDateIsGreaterThanEndDate()
        {
            // Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), StartDate = DateTime.UtcNow.AddSeconds(301), EndDate = DateTime.UtcNow.AddSeconds(180), Status = DepositionStatus.Pending, CaseId = Guid.NewGuid(), Caption = new Document() };
            var currentDepositionMock = new Deposition() { Id = Guid.NewGuid(), Status = DepositionStatus.Canceled, CaseId = Guid.NewGuid(), Caption = new Document() };
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(currentDepositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok(new Document()));
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);

            // Act
            var result = await _depositionService.ReScheduleDeposition(depositionMock, fileMock, true);

            // Assert
            Assert.True(result.IsFailed);
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Never);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()), Times.Never);
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Never);
        }

        [Fact]
        public async Task ReScheduleDeposition_ShouldReturnOk_IfStartDateIsValid()
        {
            // Arrange
            var depositionMock = new Deposition() { Id = Guid.NewGuid(), StartDate = DateTime.UtcNow.AddSeconds(400), EndDate = DateTime.UtcNow.AddSeconds(600), Status = DepositionStatus.Pending, CaseId = Guid.NewGuid(), Caption = new Document(), Case = new Case { Name = "test" } };
            var currentDepositionMock = new Deposition() { Id = Guid.NewGuid(), StartDate = DateTime.UtcNow.AddDays(-1), TimeZone = "America/New_York", Status = DepositionStatus.Canceled, CaseId = Guid.NewGuid(), Caption = new Document(), Case = new Case { Name = "test" }, Room = RoomFactory.GetRoomById(Guid.NewGuid())};
            var participant = new Participant { Name = "Jhon", Email = "test@test.com" };
            currentDepositionMock.Participants = new List<Participant> { participant };
            depositionMock.Participants = currentDepositionMock.Participants;
            depositionMock.TimeZone = "America/New_York";
            var testPath = $"{depositionMock.CaseId}/caption";
            var testEmail = "test@test.com";
            var fileName = "testFile.pdf";
            var keyName = $"/{testPath}/{fileName}";
            var userMock = new User() { EmailAddress = testEmail };
            var fileMock = new FileTransferInfo() { Name = fileName };
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(userMock);
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(currentDepositionMock);
            _documentServiceMock.Setup(dc => dc.UploadDocumentFile(It.IsAny<FileTransferInfo>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok(new Document()));
            _depositionRepositoryMock.Setup(d => d.Update(It.IsAny<Deposition>())).ReturnsAsync(depositionMock);

            // Act
            var result = await _depositionService.ReScheduleDeposition(depositionMock, fileMock, true);

            // Assert
            Assert.True(result.IsSuccess);
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(i => i == depositionMock.Id), It.IsAny<string[]>()), Times.Once);
            _documentServiceMock.Verify(dc => dc.UploadDocumentFile(
                It.IsAny<FileTransferInfo>(),
                It.Is<User>(u => u == userMock),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Once);
        }

        [Fact]
        public async Task NotifyParties_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition with id {depositionId} not found.";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionService.NotifyParties(depositionId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));

        }

        [Fact]
        public async Task NotifyParties_ShouldFail_ParticipantsNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId,
                Participants = new List<Participant>()
            };
            var expectedError = $"The deposition {depositionId} must have participants";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionService.NotifyParties(depositionId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));

        }

        [Fact]
        public async Task NotifyParties_ShouldFail_WitnessNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId,
                Participants = new List<Participant>()
                {
                    new Participant(){ Role = ParticipantType.CourtReporter }
                }
            };
            var expectedError = $"The Deposition {depositionId} must have a witness";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionService.NotifyParties(depositionId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));

        }

        [Fact]
        public async Task NotifyParties_ShouldFail_AwsSetTemplateEmailRequest_ThrowException()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId,
                Case = new Case() { Name = "TestCase" },
                StartDate = DateTime.UtcNow,
                TimeZone = "EST",
                Participants = new List<Participant>()
                {
                    new Participant(){ Role = ParticipantType.CourtReporter, Name = "Test Participant", Email = "testemail@test.com" },
                    new Participant(){ Role = ParticipantType.Witness, Name = "Test Witness" }
                }
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            //Act
            var result = await _depositionService.NotifyParties(depositionId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            _awsEmailServiceMock.Verify(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.IsType<bool>(result.Value);
            Assert.False(result.Value);
        }

        [Fact]
        public async Task NotifyParties_ShouldOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId,
                Case = new Case() { Name = "TestCase" },
                StartDate = DateTime.UtcNow,
                TimeZone = "EST",
                Participants = new List<Participant>()
                {
                    new Participant(){ Role = ParticipantType.CourtReporter, Name = "Test Participant", Email = "testemail@test.com" },
                    new Participant(){ Role = ParticipantType.Witness, Name = "Test Witness" }
                }
            };
            var expectedError = $"The Deposition {depositionId} must have a witness";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionService.NotifyParties(depositionId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            _awsEmailServiceMock.Verify(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.IsType<bool>(result.Value);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task LockBreakRoom_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition with id {depositionId} not found.";
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionService.LockBreakRoom(depositionId, Guid.NewGuid(), true);

            //Assert
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(x => x == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.NotNull(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Summary_ShouldReturnOk()
        {
            // Arrange
            var depositionId = new Guid();
            var deposition = Mock.Of<Deposition>();
            deposition.Participants = new List<Participant>() {
                Mock.Of<Participant>()
            };
            deposition.TimeZone = "America/New_York";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.Summary(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == deposition.Id), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<DepositionStatusDto>>(result);
            Assert.True(result.IsSuccess);
            var depoStatus = result.Value;
            Assert.Equal(deposition.AddedById, deposition.AddedById);
        }

        [Fact]
        public async Task Summary_ShouldFail_InvalidDepositionId()
        {
            // Arrange
            var depositionId = new Guid();
            var deposition = Mock.Of<Deposition>();
            var msg = $"Invalid Deposition id: {depositionId}";
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition) null);

            // Act
            var result = await _depositionService.Summary(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == deposition.Id), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<DepositionStatusDto>>(result);
            Assert.True(result.IsFailed);
            Assert.Equal(result.GetErrorMessage(), msg);
        }

        [Fact]
        public async Task SummaryDeposition_ShouldMapperFail()
        {
            // Arrange
            var msg = "DepositionService.Summary - Mapper failed - Value cannot be null. (Parameter 'source')";
            var depositionId = new Guid();
            var deposition = Mock.Of<Deposition>();
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.Summary(depositionId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == deposition.Id), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<DepositionStatusDto>>(result);
            Assert.True(result.IsFailed);
            Assert.Equal(result.GetErrorMessage(), msg);
        }

        [Fact]
        public async Task StampMediaDocument_ShouldFail_InvalidDepositionId()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var msg = "Could not find any deposition with Id";
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(It.IsAny<Deposition>());

            // Act
            var result = await _depositionService.StampMediaDocument(depositionId, It.IsAny<string>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(msg, result.GetErrorMessage());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.IsAny<Deposition>()), Times.Never);
        }

        [Fact]
        public async Task StampMediaDocument_ShouldFail_NoSharingDocument()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, Guid.NewGuid());
            var msg = "There is no shared document for deposition";
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.StampMediaDocument(depositionId, It.IsAny<string>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(msg, result.GetErrorMessage());
            _depositionRepositoryMock.Verify(mock => mock.Update(It.IsAny<Deposition>()), Times.Never);
        }

        [Fact]
        public async Task StampMediaDocument_ShouldUpdateLabel()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, Guid.NewGuid());
            deposition.SharingDocumentId = Guid.NewGuid();
            var stampLabel = "EXH-223";
            _depositionRepositoryMock
                .Setup(mock => mock.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(deposition);
            deposition.SharingMediaDocumentStamp = stampLabel;
            _depositionRepositoryMock
                .Setup(mock => mock.Update(It.IsAny<Deposition>()))
                .ReturnsAsync(deposition);

            // Act
            var result = await _depositionService.StampMediaDocument(depositionId, stampLabel);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Deposition>>(result);
            Assert.Equal(stampLabel, result.Value.SharingMediaDocumentStamp);
            _depositionRepositoryMock.Verify(mock => mock.Update(It.IsAny<Deposition>()), Times.Once);
        }

        [Fact]
        public async Task GetUserParticipant_ResultOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var currentUser = new User
            {
                Id = userId
            };

            var participant = new Participant
            {
                DepositionId = depositionId,
                UserId = userId
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
            _participantRepositoryMock.Setup(x => x.Update(It.IsAny<Participant>())).ReturnsAsync(participant);

            // Act
            var result = await _depositionService.GetUserParticipant(depositionId);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Participant>>(result);
            Assert.Equal(result.Value.User, currentUser);
        }

        [Fact]
        public async Task GetUserParticipant_ErrorMsg()
        {
            var errorMessage = $"User not found";

            //Arrange
            var depositionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((User)null);

            // Act
            var result = await _depositionService.GetUserParticipant(depositionId);

            //Assert
            Assert.Equal(result.Errors[0].Message, errorMessage);
            Assert.True(result.IsFailed);
        }
    }
}
