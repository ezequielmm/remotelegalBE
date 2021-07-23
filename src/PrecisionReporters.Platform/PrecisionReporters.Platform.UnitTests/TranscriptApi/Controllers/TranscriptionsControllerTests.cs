using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Controllers;
using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using PrecisionReporters.Platform.UnitTests.Utils;

namespace PrecisionReporters.Platform.UnitTests.TranscriptApi.Controllers
{
    public class TranscriptionsControllerTests
    {
        private readonly Mock<ITranscriptionService> _transcriptionServiceMock;
        private readonly Mock<IMapper<Transcription, TranscriptionDto, object>> _transcriptionMapperMock;
        private readonly Mock<IMapper<Document, DocumentDto, CreateDocumentDto>> _documentMapperMock;

        private readonly TranscriptionsController _transcriptionController;
        public TranscriptionsControllerTests()
        {
            _transcriptionServiceMock = new Mock<ITranscriptionService>();
            _transcriptionMapperMock = new Mock<IMapper<Transcription, TranscriptionDto, object>>();
            _documentMapperMock = new Mock<IMapper<Document, DocumentDto, CreateDocumentDto>>();

            _transcriptionController = new TranscriptionsController(_transcriptionServiceMock.Object, _transcriptionMapperMock.Object, _documentMapperMock.Object);
        }

        [Fact]
        public async Task GetTranscription_ShouldOk()
        {
            // Arrange
            var id = new Guid();
            var transcriptions = new List<Transcription>() {
                new Transcription()
            };
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsByDepositionId(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(transcriptions));

            //Act
            var result = await _transcriptionController.GetTranscriptions(id);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsByDepositionId(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscription_ShouldFail_GetTranscriptionsByDepositionId()
        {
            // Arrange
            var id = new Guid();
            var depos = Result.Fail(new Error());
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsByDepositionId(It.IsAny<Guid>())).ReturnsAsync(depos);

            //Act
            var result = await _transcriptionController.GetTranscriptions(id);

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsByDepositionId(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptionsFiles_ShouldOk()
        {
            // Arrange
            var id = new Guid();
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _transcriptionController.ControllerContext = context;
            var deposDocs = new List<DepositionDocument>() {
                new DepositionDocument()
            };
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsFiles(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(Result.Ok(deposDocs));

            //Act
            var result = await _transcriptionController.GetTranscriptionsFiles(id);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsFiles(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptionsFiles_ShouldFail_GetTranscriptionsFiles()
        {
            // Arrange
            var id = new Guid();
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _transcriptionController.ControllerContext = context;
            var deposDocs = Result.Fail(new Error());
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsFiles(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(deposDocs);

            //Act
            var result = await _transcriptionController.GetTranscriptionsFiles(id);

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsFiles(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptionsTime_ShouldOk()
        {
            // Arrange
            var id = new Guid();
            var transcriptionsTimes = new List<TranscriptionTimeDto>() {
                new TranscriptionTimeDto()
            };
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsWithTimeOffset(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(transcriptionsTimes));

            //Act
            var result = await _transcriptionController.GetTranscriptionsTime(id);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsWithTimeOffset(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptionsTime_ShouldFail_GetTranscriptionsWithTimeOffset()
        {
            // Arrange
            var id = new Guid();
            var transcriptionsTimes = Result.Fail(new Error());
            _transcriptionServiceMock.Setup(t => t.GetTranscriptionsWithTimeOffset(It.IsAny<Guid>())).ReturnsAsync(transcriptionsTimes);

            //Act
            var result = await _transcriptionController.GetTranscriptionsTime(id);

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _transcriptionServiceMock.Verify(mock => mock.GetTranscriptionsWithTimeOffset(It.IsAny<Guid>()), Times.Once);
        }
    }
}
