using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DepositionDocumentServiceTests : IDisposable
    {
        private readonly DepositionDocumentService _depositionDocumentService;
        private readonly Mock<IDepositionDocumentRepository> _depositionDocumentRepositoryMock;
        private readonly Mock<IAnnotationEventService> _annotationEventServiceMock;
        private readonly Mock<IDocumentService> _documentServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;

        private readonly List<DepositionDocument> _depositionDocuments = new List<DepositionDocument>();

        public DepositionDocumentServiceTests()
        {
            // SetUp
            _depositionDocumentRepositoryMock = new Mock<IDepositionDocumentRepository>();
            _depositionDocumentRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_depositionDocuments);
            _annotationEventServiceMock = new Mock<IAnnotationEventService>();
            _documentServiceMock = new Mock<IDocumentService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _userServiceMock = new Mock<IUserService>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();            
            _depositionDocumentService = new DepositionDocumentService(_depositionDocumentRepositoryMock.Object, _annotationEventServiceMock.Object, _documentServiceMock.Object, _depositionServiceMock.Object, _userServiceMock.Object, _transactionHandlerMock.Object);
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

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(document.Id), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionId), Times.Once());
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CloseDepositionDocument_CanCloseDocument_ReturnFail()
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
            var errorMessage = "403";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.Is<Guid>(a => a == depositionId), It.IsAny<string>())).ReturnsAsync(Result.Ok(participant));
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(document.Id)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseDepositionDocument(document, depositionId);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnOk()
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
            _documentServiceMock.Setup(x => x.UpdateDocument(It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok());
            _documentServiceMock.Setup(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId))).ReturnsAsync(Result.Ok());
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId)).ReturnsAsync(Result.Ok());
            
            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, file);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionDocumentRepositoryMock.Verify(x => x.Create(It.IsAny<DepositionDocument>()), Times.Once());
            _documentServiceMock.Verify(x => x.UpdateDocument(It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>(), It.IsAny<DocumentType>()), Times.Once());
            _documentServiceMock.Verify(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId)), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId), Times.Once());
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnTransactionFail()
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
            _documentServiceMock.Setup(x => x.UpdateDocument(It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>(), It.IsAny<DocumentType>())).ReturnsAsync(Result.Ok());
            _documentServiceMock.Setup(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId))).ReturnsAsync(Result.Ok());
            _annotationEventServiceMock.Setup(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId)).ReturnsAsync(Result.Ok());
            _depositionServiceMock.Setup(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId)).ReturnsAsync(Result.Ok());

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, file);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionDocumentRepositoryMock.Verify(x => x.Create(It.IsAny<DepositionDocument>()), Times.Once());
            _documentServiceMock.Verify(x => x.UpdateDocument(It.IsAny<DepositionDocument>(), It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>(), It.IsAny<DocumentType>()), Times.Once());
            _documentServiceMock.Verify(x => x.RemoveDepositionUserDocuments(It.Is<Guid>(a => a == depositionDocument.DocumentId)), Times.Once());
            _annotationEventServiceMock.Verify(x => x.RemoveUserDocumentAnnotations(depositionDocument.DocumentId), Times.Once());
            _depositionServiceMock.Verify(x => x.ClearDepositionDocumentSharingId(depositionDocument.DepositionId), Times.Once());
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
        }

        [Fact]
        public async Task CloseStampedDepositionDocument_CanCloseStampedDocument_ReturnForbidden()
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
            var errorMessage = "403";

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            _depositionServiceMock.Setup(x => x.GetDepositionParticipantByEmail(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(participant));           

            // Act
            var result = await _depositionDocumentService.CloseStampedDepositionDocument(document, depositionDocument, user.EmailAddress, file);

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once());
            _depositionServiceMock.Verify(x => x.GetDepositionParticipantByEmail(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once());            
            
            Assert.True(result.IsFailed);
            Assert.True(result.Errors[0].Message.Equals(errorMessage));
        }
    }
}
