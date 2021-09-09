using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class DocumentsControllerTest
    {
        private readonly Mock<IDocumentService> _documentService;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;
        private readonly DocumentsController _documentsController;
        public DocumentsControllerTest()
        {
            _documentService = new Mock<IDocumentService>();
            _documentMapper = new DocumentMapper();
            _documentsController = new DocumentsController(_documentService.Object, _documentMapper);
        }
        [Fact]
        public async Task UploadFiles_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            //var documentResponse = new Document { };
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.UploadDocuments(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<List<FileTransferInfo>>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Ok());
            // Act
            var result = await _documentsController.UploadFiles(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _documentService.Verify(mock => mock.UploadDocuments(It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<List<FileTransferInfo>>(),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Once);
        }
        [Fact]
        public async Task UploadFiles_ReturnsBadRequest_WhenThereIsNotFileInRequest()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            // Act
            var result = await _documentsController.UploadFiles(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
            _documentService.Verify(mock => mock.UploadDocuments(It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<List<FileTransferInfo>>(),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Never);
        }
        [Fact]
        public async Task UploadFiles_ReturnsError_WhenUploadDocumentsFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.UploadDocuments(It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<List<FileTransferInfo>>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentType>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.UploadFiles(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.UploadDocuments(It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<List<FileTransferInfo>>(),
                It.IsAny<string>(),
                It.IsAny<DocumentType>()), Times.Once);
        }

        [Fact]
        public async Task GetMyExhibits_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            var document = DocumentFactory.GetDocument();
            _documentService
                .Setup(mock => mock.GetExhibitsForUser(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new List<Document> { document }));

            // Act
            var result = await _documentsController.GetMyExhibits(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<DocumentDto>>(okResult.Value);
        }

        [Fact]
        public async Task GetMyExhibits_ReturnsError_WhenFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var document = DocumentFactory.GetDocument();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetExhibitsForUser(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.GetMyExhibits(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetExhibitsForUser(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrl_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var fileURL = "testing.com";
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetCannedPrivateURL(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(fileURL));
            // Act
            var result = await _documentsController.GetFileSignedUrl(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            // Dentro hay objecto
            Assert.IsType<ActionResult<FileSignedDto>>(result);
            _documentService.Verify(mock => mock.GetCannedPrivateURL(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetFileSignedUrl_ReturnsError_WhenFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetCannedPrivateURL(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.GetFileSignedUrl(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetCannedPrivateURL(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task ShareDocument_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.Share(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            // Act
            var result = await _documentsController.ShareDocument(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _documentService.Verify(mock => mock.Share(It.IsAny<Guid>(),
                It.IsAny<string>()), Times.Once);
        }
        [Fact]
        public async Task ShareDocument_ReturnsError_WhenShareFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var documentResponse = DocumentFactory.GetDocument();
            ContextFactory.AddUserToContext(context.HttpContext, "mock@mail.com");
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.Share(It.IsAny<Guid>(),
                    It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.ShareDocument(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.Share(It.IsAny<Guid>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetDocument_ReturnsOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();

            var context = ContextFactory.GetControllerContext();

            var document = DocumentFactory.GetDocument();

            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(document));
            // Act
            var result = await _documentsController.GetDocument(documentId);
            // Assert
            Assert.NotNull(result.Result);
            Assert.IsType<OkObjectResult>(result.Result);
            _documentService.Verify(mock => mock.GetDocument(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDocument_ReturnsError_WhenUploadDocumentsFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetDocument(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.GetDocument(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result.Result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetDocument(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMyExhibits_ReturnsOk()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();


            var context = ContextFactory.GetControllerContext();

            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.RemoveDepositionDocument(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());
            // Act
            var result = await _documentsController.DeleteMyExhibits(depositionId, documentId);
            // Assert
            Assert.NotNull(result.Result);
            Assert.IsType<OkResult>(result.Result);
            _documentService.Verify(mock => mock.RemoveDepositionDocument(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMyExhibits_ReturnsError_WhenUploadDocumentsFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.RemoveDepositionDocument(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.DeleteMyExhibits(It.IsAny<Guid>(), It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result.Result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.RemoveDepositionDocument(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task FrontendContent_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var signedDtoList = new List<FileSignedDto>
            { 
                new FileSignedDto 
                {
                   Url = "mock.com",
                   IsPublic = true,
                   Name = "randomname"
                }
            };
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetFrontEndContent())
                .ReturnsAsync(Result.Ok(signedDtoList));
            // Act
            var result = await _documentsController.FrontendContent();
            // Assert
            Assert.NotNull(result.Result);
            Assert.IsType<OkObjectResult>(result.Result);
            _documentService.Verify(mock => mock.GetFrontEndContent(), Times.Once);
        }

        [Fact]
        public async Task FrontendContent_ReturnsError_WhenUploadDocumentsFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetFrontEndContent())
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.FrontendContent();
            // Assert
            Assert.NotNull(result.Result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.GetFrontEndContent(), Times.Once);
        }

        [Fact]
        public async Task UploadTranscriptionsFiles_ReturnsOk()
        {
            // Arrange
            var uploadId = Guid.NewGuid();

            var context = ContextFactory.GetControllerContextWithFile();

            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.UploadTranscriptions(It.IsAny<Guid>(),
                    It.IsAny<List<FileTransferInfo>>()))
                .ReturnsAsync(Result.Ok());
            // Act
            var result = await _documentsController.UploadTranscriptionsFiles(uploadId);
            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _documentService.Verify(mock => mock.UploadTranscriptions(It.IsAny<Guid>(),
                    It.IsAny<List<FileTransferInfo>>()), Times.Once);
        }

        [Fact]
        public async Task UploadTranscriptionsFiles_ReturnsBadRequest_WhenThereIsNotFileInRequest()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();

            _documentsController.ControllerContext = context;
            // Act
            var result = await _documentsController.UploadTranscriptionsFiles(It.IsAny<Guid>());
            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
            _documentService.Verify(mock => mock.UploadTranscriptions(It.IsAny<Guid>(), It.IsAny<List<FileTransferInfo>>()), Times.Never);
        }

        [Fact]
        public async Task PreSignUploadExhibit_ReturnsOk()
        {
            // Arrange
            var resultDto = new PreSignedUrlDto()
            { 
                Headers = new Dictionary<string, string>() {
                    { "x-amz-meta-deposition-id", "44be6946-c51f-4249-ad95-df41a7c4c63c" },
                    { "x-amz-meta-display-name", "test.pdf" },
                    { "x-amz-meta-type", ".pdf" },
                    { "x-amz-meta-document-type", "Exhibit" },
                    { "x-amz-meta-user-id", "6fc0184a-800c-4a1e-eb44-08d94b9110fd" },
                    { "x-amz-meta-case-id", "75684789-63a5-4802-946c-08d958e2416c" },
                    { "x-amz-meta-resource-id", "rc-upload-1631037245756-2" }
                },
                Url = "https://download-url.com/tets.file/4j34h2k4h242kj4h"
            };
            var filename = "test.pdf";
            var input = new PreSignedUploadUrlDto()
            {
                DepositionId = new Guid(),
                FileName = filename,
                ResourceId = "rc-upload-1631037245756-2"
            };
            var context = ContextFactory.GetControllerContextWithFile();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetPreSignedUrlUploadExhibit(input))
                .ReturnsAsync(Result.Ok(resultDto));
            // Act
            var actionResult = await _documentsController.GetPreSignUploadExhibit(input);
            
            
            // Assert
            _documentService.Verify(mock => mock.GetPreSignedUrlUploadExhibit(input), Times.Once);
            var result = actionResult.Result as OkObjectResult;
            var value = (PreSignedUrlDto) result.Value;
            Assert.Equal(resultDto.Url, value.Url);
        }

        [Fact]
        public async Task PreSignUploadExhibit_ReturnsError_FailUrlGeneration()
        {
            // Arrange
            var filename = "test.pdf";
            var input = new PreSignedUploadUrlDto()
            {
                DepositionId = new Guid(),
                FileName = filename,
                ResourceId = "rc-upload-1631037245756-2"
            };
            var context = ContextFactory.GetControllerContextWithFile();
            _documentsController.ControllerContext = context;
            _documentService
                .Setup(mock => mock.GetPreSignedUrlUploadExhibit(input))
                .ReturnsAsync(Result.Fail(new Error()));
            // Act
            var result = await _documentsController.GetPreSignUploadExhibit(input);
            // Assert
            _documentService.Verify(mock => mock.GetPreSignedUrlUploadExhibit(input), Times.Once);
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
        }
    }
}