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
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Shared.Extensions;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DepositionDocumentServiceTests : IDisposable
    {
        private readonly DocumentConfiguration _documentConfiguration;
        private readonly DepositionDocumentService _depositionDocumentService;
        private readonly Mock<IDepositionDocumentRepository> _depositionDocumentRepositoryMock;
        private readonly Mock<IAnnotationEventService> _annotationEventServiceMock;
        private readonly Mock<IDocumentService> _documentServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IDocumentRepository> _documentRepositoryMock;
        private readonly Mock<IAwsStorageService> _awsStorageServiceMock;
        private readonly Mock<IOptions<DocumentConfiguration>> _depositionDocumentConfigurationMock;
        private readonly Mock<ILogger<DepositionDocumentService>> _loggerMock;
        private readonly Mock<ISignalRDepositionManager> _signalRNotificationManagerMock;

        private readonly List<DepositionDocument> _depositionDocuments = new List<DepositionDocument>();

        public DepositionDocumentServiceTests()
        {
            // SetUp
            _documentConfiguration = new DocumentConfiguration
            {
                BucketName = "testBucket",
                MaxFileSize = 52428800,
                AcceptedFileExtensions = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".mp3", ".m4a", ".wav", ".ogg" },
                AcceptedTranscriptionExtensions = new List<string> { ".pdf", ".txt", ".ptx" },
                NonConvertToPdfExtensions = new List<string> { ".mp4", ".mov", ".mp3", ".m4a", ".wav", ".ogg" }
            };
            _depositionDocumentRepositoryMock = new Mock<IDepositionDocumentRepository>();
            _depositionDocumentRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositionDocuments);
            _annotationEventServiceMock = new Mock<IAnnotationEventService>();
            _documentServiceMock = new Mock<IDocumentService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _userServiceMock = new Mock<IUserService>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _documentRepositoryMock = new Mock<IDocumentRepository>();
            _awsStorageServiceMock = new Mock<IAwsStorageService>();
            _depositionDocumentConfigurationMock = new Mock<IOptions<DocumentConfiguration>>();
            _depositionDocumentConfigurationMock.Setup(x => x.Value).Returns(_documentConfiguration);
            _loggerMock = new Mock<ILogger<DepositionDocumentService>>();
            _signalRNotificationManagerMock = new Mock<ISignalRDepositionManager>();

            _depositionDocumentService = new DepositionDocumentService(
                _depositionDocumentRepositoryMock.Object,
                _annotationEventServiceMock.Object,
                _documentServiceMock.Object,
                _depositionServiceMock.Object,
                _userServiceMock.Object,
                _transactionHandlerMock.Object,
                _depositionRepositoryMock.Object,
                _documentRepositoryMock.Object,
                _awsStorageServiceMock.Object,
                _depositionDocumentConfigurationMock.Object,
                _loggerMock.Object,
                _signalRNotificationManagerMock.Object
                );
        }

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task ParticipantCanCloseDocument_ReturnTrue_IfParticipant_IsAdmin()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Observer
            };

            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = user,
            };

            var depositionId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            //  Act
            var result = await _depositionDocumentService.ParticipantCanCloseDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(mock => mock.GetCurrentUserAsync(), Times.Once());
            Assert.True(result);
        }

        [Fact]
        public async Task ParticipantCanCloseDocument_ReturnTrue_IfParticipant_IsOwner()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = false,
            };

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Observer
            };

            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = user,
            };

            var depositionId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            //  Act
            var result = await _depositionDocumentService.ParticipantCanCloseDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(mock => mock.GetCurrentUserAsync(), Times.Once());
            Assert.True(result);
        }

        [Fact]
        public async Task ParticipantCanCloseDocument_ReturnTrue_IfParticipant_IsCourtReporter()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = false,
            };

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Email = "participant@email.com",
                Role = ParticipantType.CourtReporter
            };

            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };

            var depositionId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>())).ReturnsAsync(Result.Ok(participant));

            //  Act
            var result = await _depositionDocumentService.ParticipantCanCloseDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(mock => mock.GetCurrentUserAsync(), Times.Once());
            _depositionServiceMock.Verify(mock => mock.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>()), Times.Once());
            Assert.True(result);
        }

        [Fact]
        public async Task ParticipantCanCloseDocument_ReturnFalse_IfParticipantResultFailed()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = false,
            };

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Observer
            };

            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "otheruser@email.com" },
            };

            var depositionId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>())).ReturnsAsync(Result.Fail("Fail"));

            //  Act
            var result = await _depositionDocumentService.ParticipantCanCloseDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(mock => mock.GetCurrentUserAsync(), Times.Once());
            _depositionServiceMock.Verify(x => x.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>()), Times.Once());
            Assert.False(result);
        }

        [Fact]
        public async Task GetEnteredExhibits_ReturnListOfDocumnets()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var documentList = new List<DepositionDocument>
            {
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName1.pdf" }, StampLabel = "Stamped1" },
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName2.pdf" }, StampLabel = "Stamped2" },
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName3.pdf" }, StampLabel = "Stamped3" },
            };

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<DepositionDocument, object>>>()))
                .ReturnsAsync(documentList);

            //  Act
            var result = await _depositionDocumentService.GetEnteredExhibits(depositionId);
            var documentResult = result.Value;
            // Assert
            _depositionDocumentRepositoryMock.Verify(mock => mock.GetByFilterOrderByThen(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<DepositionDocument, object>>>()), Times.Once());
            Assert.NotNull(documentResult);
        }

        [Fact]
        public async Task GetEnteredExhibits_ReturnEmptyList()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var documentList = new List<DepositionDocument>();

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilterOrderByThen(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<DepositionDocument, object>>>()))
                .ReturnsAsync(documentList);

            //  Act
            var result = await _depositionDocumentService.GetEnteredExhibits(depositionId);
            var documentResult = result.Value;

            // Assert
            _depositionDocumentRepositoryMock.Verify(mock => mock.GetByFilterOrderByThen(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<Expression<Func<DepositionDocument, object>>>()), Times.Once());
            Assert.True(documentResult.Count.Equals(0));
        }

        [Fact]
        public async Task CloseDepositionDocument_CanCloseDocument_ReturnOk()
        {
            var depositionId = Guid.NewGuid();
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(document.Id)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionId)).ReturnsAsync(Result.Ok());
            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<Deposition>>>>())).Returns(async (Func<Task<Result<Deposition>>> action) =>
            {
                return await action();
            });

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(document.Id), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionId), Times.Once());
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CloseDepositionDocument_ShouldFail_WhenRemoveAnnotationsFails()
        {
            var depositionId = Guid.NewGuid();
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };
            var errorMessage = "Cannot close Document Successfully.";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(document.Id)).ReturnsAsync(Result.Fail(errorMessage));
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionId)).ReturnsAsync(Result.Ok());
            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<Deposition>>>>())).Returns(async (Func<Task<Result<Deposition>>> action) =>
            {
                return await action();
            });

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(document.Id), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionId), Times.Never());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
        }


        [Fact]
        public async Task CloseDepositionDocument_ShouldFail_WhenCleanSharingIdFails()
        {
            var depositionId = Guid.NewGuid();
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };
            var errorMessage = "Cannot close Document Successfully.";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(document.Id)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionId)).ReturnsAsync(Result.Fail(errorMessage));
            _transactionHandlerMock.Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<Deposition>>>>())).Returns(async (Func<Task<Result<Deposition>>> action) =>
            {
                return await action();
            });

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(document.Id), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionId), Times.Once());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
        }

        [Fact]
        public async Task CloseDepositionDocument_CanCloseDocument_ReturnForbidden()
        {
            var depositionId = Guid.NewGuid();
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = false,
            };

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Observer
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>())).ReturnsAsync(Result.Ok(participant));
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(document.Id)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnOk()
        {
            var temporalPath = "/TemporalFiles";
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };
            var depositionDocument = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                Document = new Document { Id = Guid.NewGuid(), Name = "docName1.pdf" },
                StampLabel = "Stamped1",
                DepositionId = Guid.NewGuid()
            };
            var file = new FileTransferInfo { Name = "file.doc" };

            _transactionHandlerMock
               .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
               .Returns(async (Func<Task<Result<bool>>> action) =>
               {
                   await action();
                   return Result.Ok(true);
               });

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionDocumentRepositoryMock.Setup(x => x.Create(It.IsAny<DepositionDocument>())).ReturnsAsync(depositionDocument);
            _documentServiceMock.Setup(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentServiceMock.Setup(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId))).ReturnsAsync(Result.Ok());
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, temporalPath);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionDocumentRepositoryMock.Verify(x => x.Create(It.IsAny<DepositionDocument>()), Times.Once());
            _documentServiceMock.Verify(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _documentServiceMock.Verify(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId)), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId), Times.Once());
            Assert.True(result.IsSuccess);
            _signalRNotificationManagerMock.Verify(mock=>mock.SendNotificationToDepositionMembers(It.IsAny<Guid>(),It.IsAny<NotificationDto>()), Times.Once);
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_Mp4File_ReturnOk()
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };
            var depositionDocument = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                Document = new Document { Id = Guid.NewGuid(), Name = "video.mp4" },
                StampLabel = "Stamped1",
                DepositionId = Guid.NewGuid()
            };

            _transactionHandlerMock
               .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
               .Returns(async (Func<Task<Result<bool>>> action) =>
               {
                   await action();
                   return Result.Ok(true);
               });

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionDocumentRepositoryMock.Setup(x => x.Create(It.IsAny<DepositionDocument>())).ReturnsAsync(depositionDocument);
            _documentServiceMock.Setup(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentServiceMock.Setup(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId))).ReturnsAsync(Result.Ok());
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, "tempPath");

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionDocumentRepositoryMock.Verify(x => x.Create(It.IsAny<DepositionDocument>()), Times.Once());
            _documentServiceMock.Verify(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _documentServiceMock.Verify(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId)), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId), Times.Once());
            Assert.True(result.IsSuccess);
            _signalRNotificationManagerMock.Verify(mock=>mock.SendNotificationToDepositionMembers(It.IsAny<Guid>(),It.IsAny<NotificationDto>()), Times.Once);
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnTransactionFail()
        {
            var temporalPath = "/TemporalFiles";
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = true,
            };
            var depositionDocument = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                Document = new Document { Id = Guid.NewGuid(), Name = "docName1.pdf" },
                StampLabel = "Stamped1",
                DepositionId = Guid.NewGuid()
            };
            var file = new FileTransferInfo { Name = "file.doc" };
            var errorMessage = "false";

            _transactionHandlerMock
               .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
               .Returns(async (Func<Task<Result<bool>>> action) =>
               {
                   await action();
                   return Result.Fail(errorMessage);
               });

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionDocumentRepositoryMock.Setup(x => x.Create(It.IsAny<DepositionDocument>())).ReturnsAsync(depositionDocument);
            _documentServiceMock.Setup(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentServiceMock.Setup(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId))).ReturnsAsync(Result.Ok());
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, temporalPath);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionDocumentRepositoryMock.Verify(x => x.Create(It.IsAny<DepositionDocument>()), Times.Once());
            _documentServiceMock.Verify(x => x.UpdateDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _documentServiceMock.Verify(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId)), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId), Times.Once());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
            _signalRNotificationManagerMock.Verify(mock=>mock.SendNotificationToDepositionMembers(It.IsAny<Guid>(),It.IsAny<NotificationDto>()), Times.Once);
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnForbidden()
        {
            var temporalPath = "/TemporalFiles";
            var document = new Document
            {
                Id = Guid.NewGuid(),
                AddedBy = new User { EmailAddress = "newuser@email.com" },
            };
            var user = new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "test@email.com",
                IsAdmin = false,
            };
            var depositionDocument = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                Document = new Document { Id = Guid.NewGuid(), Name = "docName1.pdf" },
                StampLabel = "Stamped1",
                DepositionId = Guid.NewGuid()
            };
            var file = new FileTransferInfo { Name = "file.doc" };
            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Role = ParticipantType.Observer
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(participant));

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, temporalPath);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionServiceMock.Verify(x => x.GetDepositionParticipantByEmail(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once());

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task RemoveDepositionTranscript_ShouldFaild_DepositionNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var expectedError = $"Could not find any deposition with Id { depositionId}";
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" }))), Times.Once);
            Assert.True(result.IsFailed);
            Assert.NotNull(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveDepositionTranscript_ShouldFaild_DocumentNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var expectedError = $"Could not find any document with Id {documentId}";
            var deposition = new Deposition()
            {
                Id = depositionId,
                Documents = new List<DepositionDocument>()
                {
                    new DepositionDocument(){
                        Document= new Document()
                        {
                            Id=Guid.NewGuid()
                        }
                    }
                }
            };
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" }))), Times.Once);
            Assert.True(result.IsFailed);
            Assert.NotNull(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveDepositionTranscript_ShouldFaild_IfDocumentIsDraftTranscription()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var expectedError = $"Can not delete document with Id {documentId} this is not a transcript document";
            var deposition = new Deposition()
            {
                Id = depositionId,
                Documents = new List<DepositionDocument>()
                {
                    new DepositionDocument(){
                        DepositionId = depositionId,
                        DocumentId = documentId,
                        Document= new Document()
                        {
                            DocumentType = DocumentType.DraftTranscription
                        }
                    }
                }
            };
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            //Act
            var result = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" }))), Times.Once);
            Assert.True(result.IsFailed);
            Assert.NotNull(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task RemoveDepositionTranscript_ShouldFaild_TransactionFail()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var expectedError = $"Unable to delete documents";
            var deposition = new Deposition()
            {
                Id = depositionId,
                Documents = new List<DepositionDocument>()
                {
                    new DepositionDocument(){
                        DepositionId = depositionId,
                        DocumentId = documentId,
                        Document= new Document()
                        {
                            DocumentType = DocumentType.Transcription
                        }
                    }
                }
            };
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Fail(new ExceptionalError(expectedError, new Exception(expectedError)));
                });
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Fail(expectedError));
            _loggerMock.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<object>(),
               It.IsAny<Exception>(),
               It.IsAny<Func<object, Exception, string>>()));
            //Act
            var result = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Once());
            _documentRepositoryMock.Verify(x => x.Remove(It.IsAny<Document>()), Times.Once);
            _depositionDocumentRepositoryMock.Verify(x => x.Remove(It.IsAny<DepositionDocument>()), Times.Once);

            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task RemoveDepositionTranscript_ShouldReturnOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId,
                Documents = new List<DepositionDocument>()
                {
                    new DepositionDocument(){
                        DepositionId = depositionId,
                        DocumentId = documentId,
                        Document= new Document()
                        {
                            DocumentType = DocumentType.Transcription
                        }
                    }
                }
            };
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            //Act
            var result = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);

            //Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Once());
            _documentRepositoryMock.Verify(x => x.Remove(It.IsAny<Document>()), Times.Once);
            _depositionDocumentRepositoryMock.Verify(x => x.Remove(It.IsAny<DepositionDocument>()), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task IsPublicDocument_ReturnFalse_WhenDepositionDocumentResultIsNull()
        {
            // Arrange           
            _depositionDocumentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync((DepositionDocument)null);

            //  Act
            var result = await _depositionDocumentService.IsPublicDocument(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert                      
            Assert.False(result);
        }

        [Fact]
        public async Task IsPublicDocument_ReturnTrue_WhenDepositionDocumentResultIsNotNull()
        {
            // Arrange           
            _depositionDocumentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(new DepositionDocument());

            //  Act
            var result = await _depositionDocumentService.IsPublicDocument(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert                      
            Assert.True(result);
        }

        [Fact]
        public async Task BringAllToMe_ShouldFail_DepositionNotFound()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition not found with ID: {depositionId}";
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            //Act
            var result = await _depositionDocumentService.BringAllToMe(depositionId, new BringAllToMeDto());

            //Assert
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task BringAllToMe_ShouldOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition()
            {
                Id = depositionId
            };
            _depositionRepositoryMock.Setup(d => d.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _userServiceMock.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync(new User());

            //Act
            var result = await _depositionDocumentService.BringAllToMe(depositionId, new BringAllToMeDto());

            //Assert
            _depositionRepositoryMock.Verify(d => d.GetById(It.Is<Guid>(p => p == depositionId), It.IsAny<string[]>()), Times.Once);
            _userServiceMock.Verify(u => u.GetCurrentUserAsync(), Times.Once);
            _signalRNotificationManagerMock.Verify(s => s.SendNotificationToDepositionMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Once);
            Assert.True(result.IsSuccess);
        }
    }
}
