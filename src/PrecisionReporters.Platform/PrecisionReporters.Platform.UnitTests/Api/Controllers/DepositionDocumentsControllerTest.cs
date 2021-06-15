using FluentResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Document = PrecisionReporters.Platform.Data.Entities.Document;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class DepositionDocumentsControllerTest
    {
        private readonly Mock<IDepositionService> _depositionService;
        private readonly Mock<IDocumentService> _documentService;
        private readonly Mock<IDepositionDocumentService> _depositionDocumentService;
        private readonly IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> _depositionDocumentMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;
        private readonly IMapper<Document, DocumentWithSignedUrlDto, object> _documentWithSignedUrlMapper;
        private readonly Mock<IWebHostEnvironment> _hostingEnvironment;

        private readonly DepositionDocumentsController _classUnderTest;

        public DepositionDocumentsControllerTest()
        {
            _depositionService = new Mock<IDepositionService>();
            _documentService = new Mock<IDocumentService>();
            _depositionDocumentService = new Mock<IDepositionDocumentService>();
            _depositionDocumentMapper = new DepositionDocumentMapper();
            _documentMapper = new DocumentMapper();
            _documentWithSignedUrlMapper = new DocumentWithSignedUrlMapper();
            _hostingEnvironment = new Mock<IWebHostEnvironment>();

            _classUnderTest = new DepositionDocumentsController(_depositionService.Object,
                _documentService.Object,
                _depositionDocumentService.Object,
                _depositionDocumentMapper,
                _documentMapper,
                _documentWithSignedUrlMapper,
                _hostingEnvironment.Object);
        }
        [Fact]
        public async Task CloseStampedDocument_ReturnsOk()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            var stampedDocumentDto = new StampedDocumentDto { StampLabel = "Stamp Label" };
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _classUnderTest.ControllerContext = context;

            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.CloseStampedDepositionDocument(document, It.IsAny<DepositionDocument>(), identity, It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.CloseStampedDocument(It.IsAny<Guid>(), stampedDocumentDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseStampedDepositionDocument(document, It.IsAny<DepositionDocument>(), identity, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task CloseStampedDocument_ReturnsError_WhenGetSharedDocumentFail()
        {
            // Arrange
            var stampedDocumentDto = new StampedDocumentDto { StampLabel = "Stamp Label" };
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _classUnderTest.ControllerContext = context;

            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.CloseStampedDocument(It.IsAny<Guid>(), stampedDocumentDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService
                .Verify(mock => mock.CloseStampedDepositionDocument(It.IsAny<Document>(), It.IsAny<DepositionDocument>(), identity, It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CloseStampedDocument_ReturnsError_WhenCloseStampedDepositionDocumentFail()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            var stampedDocumentDto = new StampedDocumentDto { StampLabel = "Stamp Label" };
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _classUnderTest.ControllerContext = context;

            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.CloseStampedDepositionDocument(document, It.IsAny<DepositionDocument>(), identity, It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.CloseStampedDocument(It.IsAny<Guid>(), stampedDocumentDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseStampedDepositionDocument(document, It.IsAny<DepositionDocument>(), identity, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSharedDocument_ReturnsOkAndDocumentsWithSignedUrlDto()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            var signedPublicUrl = "http://mock.signed.public.url";
            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.ParticipantCanCloseDocument(document, It.IsAny<Guid>()))
                .ReturnsAsync(true);
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(document))
                .Returns(Result.Ok(signedPublicUrl));
            _depositionDocumentService
                .Setup(mock => mock.IsPublicDocument(It.IsAny<Guid>(), document.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _classUnderTest.GetSharedDocument(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DocumentWithSignedUrlDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<DocumentWithSignedUrlDto>(okResult.Value);
            Assert.Equal(signedPublicUrl, resultValue.PreSignedUrl);
            Assert.Equal(document.Id, resultValue.Id);
            Assert.Equal(document.CreationDate, resultValue.CreationDate);
            Assert.Equal(document.DisplayName, resultValue.DisplayName);
            Assert.Equal(document.DocumentType, resultValue.DocumentType);
            Assert.Equal(document.Size, resultValue.Size);
            Assert.Equal(document.AddedBy.Id, resultValue.AddedBy.Id);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService.Verify(mock => mock.ParticipantCanCloseDocument(document, It.IsAny<Guid>()), Times.Once);
            _documentService.Verify(mock => mock.GetFileSignedUrl(document), Times.Once);
            _depositionDocumentService.Verify(mock => mock.IsPublicDocument(It.IsAny<Guid>(), document.Id), Times.Once);
        }

        [Fact]
        public async Task GetSharedDocument_ReturnsError_WhenGetFileSignedUrlFails()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.ParticipantCanCloseDocument(document, It.IsAny<Guid>()))
                .ReturnsAsync(true);
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(document))
                .Returns(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetSharedDocument(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService.Verify(mock => mock.ParticipantCanCloseDocument(document, It.IsAny<Guid>()), Times.Once);
            _documentService.Verify(mock => mock.GetFileSignedUrl(document), Times.Once);
            _depositionDocumentService.Verify(mock => mock.IsPublicDocument(It.IsAny<Guid>(), document.Id), Times.Never);
        }

        [Fact]
        public async Task GetSharedDocument_ReturnsError_WhenGetSharedDocumentFails()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetSharedDocument(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetSharedDocument(It.IsAny<Guid>()), Times.Once);
            _depositionDocumentService.Verify(mock => mock.ParticipantCanCloseDocument(It.IsAny<Document>(), It.IsAny<Guid>()), Times.Never);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Document>()), Times.Never);
            _depositionDocumentService.Verify(mock => mock.IsPublicDocument(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CloseDocument_ReturnsOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(documentId))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok);

            // Act
            var result = await _classUnderTest.CloseDocument(documentId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _depositionService.Verify(mock => mock.GetSharedDocument(documentId), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CloseDocument_ReturnsNotFound_WhenGetSharedDocumentFail()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new ResourceNotFoundError("Mock Deposition not found")));

            // Act
            var result = await _classUnderTest.CloseDocument(documentId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NotFoundObjectResult>(result);
            _depositionService.Verify(mock => mock.GetSharedDocument(documentId), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseDepositionDocument(It.IsAny<Document>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CloseDocument_ReturnsServerError_WhenCloseDepositionDocumentFail()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(documentId))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail("Cannot close Document Successfully."));

            // Act
            var result = await _classUnderTest.CloseDocument(documentId);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetSharedDocument(documentId), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CloseDocument_ReturnsForbidden_WhenParticipantIsNotAllowToClose()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetSharedDocument(documentId))
                .ReturnsAsync(Result.Ok(document));
            _depositionDocumentService
                .Setup(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new ForbiddenError()));

            // Act
            var result = await _classUnderTest.CloseDocument(documentId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ForbidResult>(result);
            _depositionService.Verify(mock => mock.GetSharedDocument(documentId), Times.Once);
            _depositionDocumentService.Verify(mock => mock.CloseDepositionDocument(document, It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetEnteredExhibits_ReturnsOkAndListOfDocumentDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var exhibitSortField = ExhibitSortField.Name;
            var sortDirection = SortDirection.Ascend;
            var document = DocumentFactory.GetDocument();

            _depositionDocumentService
                .Setup(mock => mock.GetEnteredExhibits(depositionId, exhibitSortField, sortDirection))
                .ReturnsAsync(Result.Ok(new List<DepositionDocument>
                {
                    new DepositionDocument
                    {
                        Id = Guid.NewGuid(),
                        Document = document,
                        StampLabel = "Mock StampLabel"
                    }
                }));

            // Act
            var result = await _classUnderTest.GetEnteredExhibits(depositionId, exhibitSortField, sortDirection);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<List<DocumentDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<DocumentDto>>(okResult.Value);
            _depositionDocumentService.Verify(mock => mock.GetEnteredExhibits(depositionId, exhibitSortField, sortDirection), Times.Once);
        }

        [Fact]
        public async Task GetEnteredExhibits_ReturnsError()
        {
            // Arrange
            _depositionDocumentService
                .Setup(mock => mock.GetEnteredExhibits(It.IsAny<Guid>(), It.IsAny<ExhibitSortField>(), It.IsAny<SortDirection>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetEnteredExhibits(It.IsAny<Guid>(), It.IsAny<ExhibitSortField>(), It.IsAny<SortDirection>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<List<DocumentDto>>>(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionDocumentService.Verify(mock => mock.GetEnteredExhibits(It.IsAny<Guid>(), It.IsAny<ExhibitSortField>(), It.IsAny<SortDirection>()), Times.Once);
        }

        [Fact]
        public async Task ShareEnteredExhibit_ReturnsOk()
        {
            // Arrange
            _documentService
                .Setup(mock => mock.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _documentService.Verify(mock => mock.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task ShareEnteredExhibit_ReturnsError()
        {
            // Arrange
            _documentService
                .Setup(mock => mock.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.ShareEnteredExhibit(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrl_ReturnsOkAndFileSignedDto()
        {
            // Arrange
            var signedPublicUrl = "http://mock.signed.public.url";
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(signedPublicUrl));

            // Act
            var result = await _classUnderTest.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<FileSignedDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<FileSignedDto>(okResult.Value);
            Assert.Equal(signedPublicUrl, resultValue.Url);
            Assert.True(resultValue.IsPublic);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrl_ReturnsError()
        {
            // Arrange
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<FileSignedDto>>(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrlList_ReturnsOkAndFileSignedListDto()
        {
            // Arrange
            var signedPublicUrl = "http://mock.signed.public.url";
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
                .ReturnsAsync(Result.Ok(new List<string> { signedPublicUrl }));

            // Act
            var result = await _classUnderTest.GetFileSignedUrlList(It.IsAny<Guid>(), It.IsAny<List<Guid>>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsAssignableFrom<FileSignedListDto>(okResult.Value);
            Assert.Contains(resultValue.URLs, url => url.Equals(signedPublicUrl));
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrlList_ReturnsError()
        {
            // Arrange
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetFileSignedUrlList(It.IsAny<Guid>(), It.IsAny<List<Guid>>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Once);
        }

        [Fact]
        public async Task DeleteTranscript_ReturnOk()
        {
            // Arrange
            _depositionDocumentService
                .Setup(mock => mock.RemoveDepositionTranscript(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok);

            // Act
            var result = await _classUnderTest.DeleteTranscript(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _depositionDocumentService.Verify(mock => mock.RemoveDepositionTranscript(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task DeleteTranscript_ReturnsError()
        {
            // Arrange
            _depositionDocumentService
                .Setup(mock => mock.RemoveDepositionTranscript(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.DeleteTranscript(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionDocumentService.Verify(mock => mock.RemoveDepositionTranscript(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task BringAllToMe_ReturnsOk()
        {
            // Arrange
            _depositionDocumentService
                .Setup(mock => mock.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>()))
                .ReturnsAsync(Result.Ok);

            // Act
            var result = await _classUnderTest.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _depositionDocumentService.Verify(mock => mock.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>()), Times.Once);
        }

        [Fact]
        public async Task BringAllToMe_ReturnsError()
        {
            // Arrange
            _depositionDocumentService
                .Setup(mock => mock.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionDocumentService.Verify(mock => mock.BringAllToMe(It.IsAny<Guid>(), It.IsAny<BringAllToMeDto>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionCaption_ReturnsOkAndDocumentWithSignedUrlDto()
        {
            // Arrange
            var signedPublicUrl = "http://mock.signed.public.url";
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetDepositionCaption(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(document))
                .Returns(Result.Ok(signedPublicUrl));

            // Act
            var result = await _classUnderTest.GetDepositionCaption(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DocumentWithSignedUrlDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<DocumentWithSignedUrlDto>(okResult.Value);
            Assert.Equal(signedPublicUrl, resultValue.PreSignedUrl);
            Assert.Equal(document.Id, resultValue.Id);
            Assert.Equal(document.CreationDate, resultValue.CreationDate);
            Assert.Equal(document.DisplayName, resultValue.DisplayName);
            Assert.Equal(document.DocumentType, resultValue.DocumentType);
            Assert.Equal(document.Size, resultValue.Size);
            Assert.Equal(document.AddedBy.Id, resultValue.AddedBy.Id);
            _depositionService.Verify(mock => mock.GetDepositionCaption(It.IsAny<Guid>()), Times.Once);
            _documentService.Verify(mock => mock.GetFileSignedUrl(document), Times.Once);
        }

        [Fact]
        public async Task GetDepositionCaption_ReturnsError_WhenGetDepositionCaptionFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionCaption(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionCaption(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DocumentWithSignedUrlDto>>(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionCaption(It.IsAny<Guid>()), Times.Once);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Document>()), Times.Never);
        }

        [Fact]
        public async Task GetDepositionCaption_ReturnsError_WhenGetFileSignedUrlFails()
        {
            // Arrange
            var document = DocumentFactory.GetDocument();
            _depositionService
                .Setup(mock => mock.GetDepositionCaption(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            _documentService
                .Setup(mock => mock.GetFileSignedUrl(document))
                .Returns(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionCaption(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DocumentWithSignedUrlDto>>(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionCaption(It.IsAny<Guid>()), Times.Once);
            _documentService.Verify(mock => mock.GetFileSignedUrl(It.IsAny<Document>()), Times.Once);
        }
    }
}