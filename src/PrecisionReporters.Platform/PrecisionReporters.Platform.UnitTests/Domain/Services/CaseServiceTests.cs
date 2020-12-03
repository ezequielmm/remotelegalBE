using FluentResults;
using Microsoft.Extensions.Logging;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Commons;
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
    public class CaseServiceTests : IDisposable
    {
        private readonly CaseService _service;
        private readonly Mock<ICaseRepository> _caseRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IDepositionDocumentService> _depositionDocumentServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<ILogger<CaseService>> _loggerMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;

        private readonly List<Case> _cases = new List<Case>();

        public CaseServiceTests()
        {
            // Setup
            _caseRepositoryMock = new Mock<ICaseRepository>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();
            _permissionServiceMock = new Mock<IPermissionService>();

            _caseRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(_cases);
            _caseRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(() => _cases.FirstOrDefault());
            _transactionHandlerMock
                 .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            _userServiceMock = new Mock<IUserService>();
            _depositionDocumentServiceMock = new Mock<IDepositionDocumentService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _loggerMock = new Mock<ILogger<CaseService>>();
            _service = new CaseService(_caseRepositoryMock.Object, _userServiceMock.Object,
                _depositionDocumentServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object, _transactionHandlerMock.Object, _permissionServiceMock.Object);
        }

        public void Dispose()
        {
            // Tear down
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
            _caseRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, object>>>(),
                    It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(userCases);

            await _service.GetCasesForUser(userEmail, orderBy, sortDirection);

            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);

            // TODO: Find a way to evaluate that the param orderBy was called with the given field or the default one
            _caseRepositoryMock.Verify(
                x => x.GetByFilter(It.IsAny<Expression<Func<Case, object>>>(),
                    It.Is<SortDirection>(
                        a => a == sortDirection || (a == SortDirection.Ascend && sortDirection == null)),
                    It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleDepositions_ShouldReturnFail_IfUserNotFound()
        {
            // Arrange
            var userEmail = "testUser@mail.com";

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _service.ScheduleDepositions(userEmail, Guid.NewGuid(), null, null);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
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
            var caseId = Guid.NewGuid();
            var errorMessage = $"Case with id {caseId} not found.";

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Case) null);

            // Act
            var result = await _service.ScheduleDepositions(userEmail, caseId, null, null);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _caseRepositoryMock.Verify(
                x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()),
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

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Case {Id = caseId});
            _depositionDocumentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>()))
                .ReturnsAsync(Result.Fail("Unable to upload document"));

            // Act
            var result = await _service.ScheduleDepositions(userEmail, caseId, depositions, files);

            // Assert
            _caseRepositoryMock.Verify(
                x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()),
                Times.Once);
            _depositionDocumentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString()))), Times.Once);
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
            _depositionDocumentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<DepositionDocument>>()),
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

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Case {Id = caseId});
            _depositionDocumentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            _depositionServiceMock
                .Setup(x => x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()))
                .ReturnsAsync(Result.Fail(errorMessage));

            // Act
            var result = await _service.ScheduleDepositions(userEmail, caseId, depositions, files);

            // Assert
            _depositionDocumentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString()))), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()),
                Times.Once);
            _depositionDocumentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<DepositionDocument>>()),
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

            var logErrorMessage = "Unable to schedule depositions";
            var errorMessage = "Unable to schedule depositions";

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Case {Id = caseId, Depositions = new List<Deposition>()});
            _depositionDocumentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()))
                .ReturnsAsync(Result.Ok(depositions[0]))
                .ReturnsAsync(Result.Ok(depositions[1]));
            _caseRepositoryMock.Setup(x => x.Update(It.IsAny<Case>())).ThrowsAsync(new Exception("TestException"));

            // Act
            var result = await _service.ScheduleDepositions(userEmail, caseId, depositions, files);

            // Assert
            _depositionDocumentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString()))), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()),
                Times.Exactly(depositions.Count));
            _caseRepositoryMock.Verify(x => x.Update(It.Is<Case>(a => a.Id == caseId)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _depositionDocumentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<DepositionDocument>>()),
                Times.Once);
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

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _caseRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Case {Id = caseId, Depositions = new List<Deposition>()});
            _depositionDocumentServiceMock
                .Setup(x => x.UploadDocumentFile(It.IsAny<KeyValuePair<string, FileTransferInfo>>(), It.IsAny<User>(),
                    It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            _depositionServiceMock
                .SetupSequence(x =>
                    x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()))
                .ReturnsAsync(Result.Ok(depositions[0]))
                .ReturnsAsync(Result.Ok(depositions[1]));
            _caseRepositoryMock.Setup(x => x.Update(It.IsAny<Case>())).ReturnsAsync(new Case {Id = caseId});

            // Act
            var result = await _service.ScheduleDepositions(userEmail, caseId, depositions, files);

            // Assert
            _depositionDocumentServiceMock.Verify(x => x.UploadDocumentFile(
                It.IsAny<KeyValuePair<string, FileTransferInfo>>(),
                It.Is<User>(a => a == user),
                It.Is<string>(a => a.Contains(caseId.ToString()))), Times.Exactly(depositions.Count));
            _depositionServiceMock.Verify(
                x => x.GenerateScheduledDeposition(It.IsAny<Deposition>(), It.IsAny<List<DepositionDocument>>()),
                Times.Exactly(depositions.Count));
            _caseRepositoryMock.Verify(x => x.Update(It.Is<Case>(a => a.Id == caseId)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);
            _depositionDocumentServiceMock.Verify(x => x.DeleteUploadedFiles(It.IsAny<List<DepositionDocument>>()), Times.Never);
            Assert.NotNull(result);
            Assert.IsType<Result<Case>>(result);
            Assert.True(result.IsSuccess);
        }
    }
}
