﻿using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using PrecisionReporters.Platform.Domain.Configurations;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Enums;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class CaseServiceTests : IDisposable
    {
        private readonly CaseService _service;
        private readonly Mock<ICaseRepository> _caseRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IDocumentService> _documentServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<ILogger<CaseService>> _loggerMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;
        private readonly Mock<IOptions<DepositionConfiguration>> _depositionConfigurationMock;
        private readonly DepositionConfiguration _depositionconfiguration;


        private readonly List<Case> _cases = new List<Case>();

        public CaseServiceTests()
        {
            // Setup
            _caseRepositoryMock = new Mock<ICaseRepository>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();
            _permissionServiceMock = new Mock<IPermissionService>();

            _depositionconfiguration = new DepositionConfiguration { DepositionScheduleRestrictionHours = "48"};
            _depositionConfigurationMock = new Mock<IOptions<DepositionConfiguration>>();
            _depositionConfigurationMock.Setup(x => x.Value).Returns(_depositionconfiguration);

            _caseRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(_cases);
            _caseRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(() => _cases.FirstOrDefault());
            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<Case>>>>()))
                .Returns(async (Func<Task<Result<Case>>> action) =>
                {
                    return await action();
                });

            _userServiceMock = new Mock<IUserService>();
            _documentServiceMock = new Mock<IDocumentService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _loggerMock = new Mock<ILogger<CaseService>>();
            _service = new CaseService(_caseRepositoryMock.Object, _userServiceMock.Object,
                _documentServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object, _transactionHandlerMock.Object, _permissionServiceMock.Object, _depositionConfigurationMock.Object);
        }

        public void Dispose()
        {
            // Tear down
        }


        [Fact]
        public async Task GetCases_ShouldNotFilterList_WithAdminUser()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var user1 = new User
            {
                Id = user1Id,
                IsAdmin = true,
                EmailAddress = "user1@email.com"
            };
            var user2 = new User
            {
                Id = user2Id,
                IsAdmin = false,
                EmailAddress = "user2@email.com"
            };
            var existingCases = new List<Case>
            {
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = user1,
                    AddedById = user1Id
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase2",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = user1,
                    AddedById = user1Id
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase3",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = user2,
                    AddedById = user2Id
                },
            };

            //get user by email ok
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user1));

            //get all list
            _caseRepositoryMock.Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<Case, object>>>()))
                .ReturnsAsync(existingCases);

            // Act
            var result = await _service.GetCasesForUser(user1.EmailAddress);

            // Assert
            Assert.True(result.IsSuccess);
            var cases = result.Value;
            Assert.NotNull(cases);
            Assert.Equal(cases.Count(), existingCases.Count());
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == user1.EmailAddress)), Times.Once);
            _caseRepositoryMock.Verify(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.Is<Expression<Func<Case, bool>>>(x => x == null), It.IsAny<string[]>(), It.IsAny<Expression<Func<Case, object>>>()));
        }

        [Fact]
        public async Task GetCases_ShouldReturn_ListOfAllCases()
        {
            // Arrange
            _cases.AddRange(new List<Case>
            {
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1",
                    CreationDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase2",
                    CreationDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase3",
                    CreationDate = DateTime.UtcNow
                }
            });

            // Act
            var result = await _service.GetCases();

            // Assert
            _caseRepositoryMock.Verify(
                mock => mock.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.NotEmpty(result);
            Assert.Equal(_cases.Count, result.Count);
        }

        [Fact]
        public async Task GetCaseById_ShouldReturn_CasesWithGivenId()
        {
            // Arrange
            var id = Guid.NewGuid();
            _cases.Add(new Case
            {
                Id = id,
                Name = "TestCase1",
                CreationDate = DateTime.Now
            });

            // Act
            var result = await _service.GetCaseById(id);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()),
                Times.Once());
            Assert.True(result.IsSuccess);

            var foundCase = result.Value;
            Assert.NotNull(foundCase);
            Assert.Equal(id, foundCase.Id);
        }

        [Fact]
        public async Task CreateCase_ShouldReturn_NewlyCreatedCase_WithGivenName()
        {
            // Arrange
            var name = "Test";
            var userEmail = "TestUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var newCase = new Case { CreationDate = DateTime.Now, Name = name };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock.Setup(x => x.Create(It.IsAny<Case>()))
                .Returns<Case>((a) =>
                {
                    a.Id = Guid.NewGuid();
                    return Task.FromResult(a);
                })
                .Verifiable();
            _permissionServiceMock.Setup(s => s.AddUserRole(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ResourceType>(), It.IsAny<RoleName>())).Returns(Task.FromResult(Result.Ok()));

            // Act
            var result = await _service.CreateCase(userEmail, newCase);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _caseRepositoryMock.Verify(mock => mock.Create(It.Is<Case>(a => a == newCase)), Times.Once());
            Assert.True(result.IsSuccess);

            var createdCase = result.Value;
            Assert.NotNull(createdCase);
            Assert.Equal(name, createdCase.Name);
            _permissionServiceMock.Verify(m => m.AddUserRole(It.Is<Guid>(x => x == user.Id), It.Is<Guid>(x => x == createdCase.Id), It.Is<ResourceType>(x => x == ResourceType.Case), It.Is<RoleName>(x => x == RoleName.CaseAdmin)));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(CaseSortField.AddedBy, SortDirection.Ascend)]
        [InlineData(CaseSortField.Name, SortDirection.Descend)]
        [InlineData(CaseSortField.CaseNumber, SortDirection.Ascend)]
        [InlineData(CaseSortField.CreatedDate, null)]
        public async Task GetCasesForUser_ShouldReturn_ListOfCases_WhereLogedUserIsMemberOf(CaseSortField? orderBy,
            SortDirection? sortDirection)
        {
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var userCases = new List<Case>
            {
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1",
                    CreationDate = DateTime.UtcNow
                }
            };

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock.Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(),
                    It.IsAny<Expression<Func<Case, object>>>()))
                .ReturnsAsync(userCases);

            await _service.GetCasesForUser(userEmail, orderBy, sortDirection);

            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);

            // TODO: Find a way to evaluate that the param orderBy was called with the given field or the default one
            _caseRepositoryMock.Verify(
                x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.Is<SortDirection>(
                        a => a == sortDirection || (a == SortDirection.Ascend && sortDirection == null)),
                    It.IsNotNull<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<Case, object>>>()), Times.Once);
        }

        [Fact]
        public async Task GetCasesForUser_ShouldOrderByThen_WhenSortedFieldIsAddedBy()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var userCases = CaseFactory.GetCases();

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock.Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(),
                    It.IsAny<Expression<Func<Case, object>>>()))
                .ReturnsAsync(userCases);

            // Act
            var result = await _service.GetCasesForUser(userEmail, CaseSortField.AddedBy, SortDirection.Ascend);

            // Assert
            _caseRepositoryMock.Verify(r => r.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(),
                    It.Is<Expression<Func<Case, object>>>(x => x != null)));
        }

        [Fact]
        public async Task GetCasesForUser_ShouldReturnOrderedCasesListByAdded_WhenSortDirectionIsAscendAndSortedFieldIsAddedBy()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var userCases = CaseFactory.GetCases();
            var sortedUserCasesList = userCases.OrderBy(x => x.AddedBy.FirstName).ThenBy(x => x.AddedBy.LastName).ToList();
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));

            _caseRepositoryMock.Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(),
                    It.IsAny<Expression<Func<Case, object>>>()))
                .ReturnsAsync(sortedUserCasesList);

            // Act
            var result = await _service.GetCasesForUser(userEmail, CaseSortField.AddedBy, SortDirection.Ascend);

            // Assert
            Assert.True(sortedUserCasesList.SequenceEqual(result.Value));
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfUserNotFound()
        {
            // Arrange
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((User)null);

            // Act
            var result = await _service.ScheduleDepositions(Guid.NewGuid(), null, null);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfCaseNotFound()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            user.IsAdmin = true;
            var caseId = Guid.NewGuid();
            var errorMessage = $"Case with id {caseId} not found.";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Case)null);

            // Act
            var result = await _service.ScheduleDepositions(caseId, new List<Deposition>(), null);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _caseRepositoryMock.Verify(
                x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfFileCantUpload()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            user.IsAdmin = true;
            var caseId = Guid.NewGuid();
            var depositions = DepositionFactory.GetDepositionList();
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            var logErrorMessage = "Unable to load one or more documents to storage";
            var logInformationMessage = "Removing uploaded documents";
            var errorMessage = "Unable to upload one or more documents to deposition";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Fail("Unable to upload document"));

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            _caseRepositoryMock.Verify(
                x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()),
                Times.Once);
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(a => a == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == logInformationMessage),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfGenerateScheduleDepositionFails()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var caseId = Guid.NewGuid();
            var depositions = DepositionFactory.GetDepositionList();
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            var errorMessage = "TestErrorMessageResult";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok(new Document()));
            _depositionServiceMock
                .Setup(x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()))
                .ReturnsAsync(Result.Fail(errorMessage));

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()),
                Times.Once);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfDepositionIsGeneratedWithin48Hours()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var caseId = Guid.NewGuid();
            var depositions = new List<Deposition> 
            { 
                new Deposition
                {
                    Id = Guid.Parse("ecd125d5-cb5e-4b8a-91c3-830a8ea7270f"),
                    StartDate = DateTime.UtcNow.AddHours(3),
                    EndDate = DateTime.UtcNow.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                    TimeZone = "America/New_York"
                } 
            };
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            var errorMessage = "IF YOU ARE BOOKING A DEPOSITION WITHIN 48 HOURS";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok(new Document()));
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(depositions[0]));

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()),
                Times.Once);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfDepositionIsGeneratedByAdminInWeekend()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = userEmail,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsAdmin = true
            };
            var caseId = Guid.NewGuid();
            var depositions = new List<Deposition> 
            { 
                new Deposition
                {
                    Id = Guid.Parse("ecd125d5-cb5e-4b8a-91c3-830a8ea7270f"),
                    StartDate = new DateTime(2021, 09, 25, 20, 00, 00),
                    EndDate = DateTime.UtcNow.AddHours(3),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness, IsAdmitted = true } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                    IsOnTheRecord = true,
                    TimeZone = "America/New_York"
                } 
            };
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            var errorMessage = "IF YOU ARE BOOKING A DEPOSITION WITHIN 48 HOURS";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok(new Document()));
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(depositions[0]));

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()),
                Times.Once);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfCaseRepositoryUpdateThrowsException()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            user.IsAdmin = true;
            var caseId = Guid.NewGuid();
            var depositions = new List<Deposition> 
            {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddHours(5),
                    Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User() { EmailAddress = "testUser@mail.com" },
                    TimeZone = "America/New_York"
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddHours(5),
                    Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User() { EmailAddress = "testUser@mail.com" },
                    TimeZone = "America/New_York"
                }
            };
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            var logErrorMessage = "Unable to schedule depositions";
            var errorMessage = "Unable to schedule depositions";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId, Depositions = new List<Deposition>(), Members = new List<Member>() });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok(new Document()));
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(depositions[0]))
                .ReturnsAsync(Result.Ok(depositions[1]));
            _caseRepositoryMock.Setup(x => x.Update(It.IsAny<Case>())).ThrowsAsync(new Exception("TestException"));

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()),
                Times.Exactly(depositions.Count));
            _caseRepositoryMock.Verify(x => x.Update(It.Is<Case>(a => a.Id == caseId)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()),
                Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfUserIsAdmin_RequesterIsMissing()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            user.IsAdmin = true;
            var depositions = new List<Deposition>()
            {
                new Deposition(){Requester=new User() }
            };
            var caseId = Guid.NewGuid();
            var errorMessage = $"Requester information missing";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, null);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfUserIsNotAdmin_InvalidParticipantRole()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var participants = new List<Participant>()
            {
                new Participant()
                {
                    Role = ParticipantType.CourtReporter
                }
            };
            var depositions = new List<Deposition>()
            {
                new Deposition()
                {
                    Participants = participants
                }
            };
            var caseId = Guid.NewGuid();
            var errorMessage = $"Can not assign this role to the participants";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, null);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturn_UpdatedCase()
        {
            // Arrange
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            user.IsAdmin = true;
            var caseId = Guid.NewGuid();
            var depositions = new List<Deposition> 
            {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = new DateTime(2021, 10, 18, 17, 00, 00),
                    EndDate = DateTime.UtcNow.AddHours(5),
                    Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User() { EmailAddress = "testUser@mail.com" },
                    TimeZone = "America/New_York"
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = new DateTime(2021, 10, 18, 17, 00, 00),
                    EndDate = DateTime.UtcNow.AddHours(5),
                    Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User() { EmailAddress = "testUser@mail.com" },
                    TimeZone = "America/New_York"
                }
            };
            var files = new Dictionary<string, FileTransferInfo>();
            for (int i = 1; i <= depositions.Count; i++)
            {
                var fileKey = $"testFileKey{i}";
                depositions[i - 1].FileKey = fileKey;
                var file = new FileTransferInfo
                {
                    Name = $"file{i}",
                    Length = 1000
                };
                files.Add(fileKey, file);
            }

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new Case { Id = caseId, Depositions = new List<Deposition>(), Members = new List<Member>() });
            _documentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>(), It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok(new Document()));
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(depositions[0]))
                .ReturnsAsync(Result.Ok(depositions[1]));
            _caseRepositoryMock.Setup(x => x.Update(It.IsAny<Case>())).ReturnsAsync(new Case { Id = caseId });

            // Act
            var result = await _service.ScheduleDepositions(caseId, depositions, files);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsSuccess);
            _documentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString())),
                It.IsAny<DocumentType>()), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Guid>(), It.IsAny<Deposition>(), It.IsAny<List<Document>>(), It.IsAny<User>()),
                Times.Exactly(depositions.Count));
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);
            _documentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<Document>>()), Times.Never);
        }

        [Fact]
        public async Task EditCase_ShouldReturnOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var oldCase = new Case
            {
                Id = id,
                Name = "TestCase1",
                CreationDate = DateTime.Now,
                CaseNumber = $"#{short.MaxValue}"
            };
            _cases.Add(oldCase);
            var editCase = new Case
            {
                Id = id,
                CaseNumber = $"#{int.MaxValue}",
                Name = "Different CaseName"
            };

            _caseRepositoryMock
                .Setup(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()))
                .ReturnsAsync(oldCase);
            _caseRepositoryMock
                .Setup(mock => mock.Update(It.IsAny<Case>()))
                .ReturnsAsync(new Case { Id = oldCase.Id, Name = editCase.Name, CaseNumber = editCase.CaseNumber, CreationDate = oldCase.CreationDate });
            // Act
            var result = await _service.EditCase(editCase);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()), Times.Once());
            _caseRepositoryMock.Verify(mock => mock.Update(It.IsAny<Case>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count == 0);
        }

        [Fact]
        public async Task EditCase_ShouldReturnResourceNotFoundError_WhenCaseNotFound()
        {
            // Arrange
            var errorMessage = "Case with id";
            var id = Guid.NewGuid();
            var editCase = new Case
            {
                Id = id,
                CaseNumber = $"#{int.MaxValue}",
                Name = "Different CaseName"
            };

            // Act
            var result = await _service.EditCase(editCase);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()), Times.Once());
            _caseRepositoryMock.Verify(mock => mock.Update(It.IsAny<Case>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task EditCase_ShouldReturnResourceNotFoundError_WhenFailsToEditCase()
        {
            // Arrange
            var errorMessage = "There was an error updating Case with Id:";
            var id = Guid.NewGuid();
            var oldCase = new Case
            {
                Id = id,
                Name = "TestCase1",
                CreationDate = DateTime.Now,
                CaseNumber = $"#{short.MaxValue}"
            };
            _cases.Add(oldCase);
            var editCase = new Case
            {
                Id = id,
                CaseNumber = $"#{int.MaxValue}",
                Name = "Different CaseName"
            };

            _caseRepositoryMock
                .Setup(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()))
                .ReturnsAsync(oldCase);

            // Act
            var result = await _service.EditCase(editCase);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string[]>()), Times.Once());
            _caseRepositoryMock.Verify(mock => mock.Update(It.IsAny<Case>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }
    }
}
