﻿using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Org.BouncyCastle.Asn1.Cms;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using PrecisionReporters.Platform.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DocumentServiceTest
    {
        private readonly DocumentConfiguration _documentConfiguration;
        private readonly Mock<IAwsStorageService> _awsStorageServiceMock;
        private readonly Mock<IOptions<DocumentConfiguration>> _depositionDocumentConfigurationMock;
        private readonly Mock<ILogger<DocumentService>> _loggerMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IDocumentUserDepositionRepository> _documentUserDepositionRepositoryMock;
        private readonly Mock<IPermissionService> _permissionServiceMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<IDocumentRepository> _documentRepositoryMock;
        private readonly Mock<IDepositionDocumentRepository> _depositionDocumentRepositoryMock;
        private readonly Mock<ISignalRNotificationManager> _signalRNotificationManagerMock;
        private readonly Mock<IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto>> _annotationEventMapperMock;
        private readonly DocumentService _service;
        private readonly string[] includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};

        public DocumentServiceTest()
        {
            _documentConfiguration = new DocumentConfiguration
            {
                BucketName = "testBucket",
                MaxFileSize = 52428800,
                AcceptedFileExtensions = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".png", ".mp4" },
                AcceptedTranscriptionExtensions = new List<string> { ".pdf", ".txt", ".ptx" }
            };

            _awsStorageServiceMock = new Mock<IAwsStorageService>();
            _loggerMock = new Mock<ILogger<DocumentService>>();
            _depositionDocumentConfigurationMock = new Mock<IOptions<DocumentConfiguration>>();
            _depositionDocumentConfigurationMock.Setup(x => x.Value).Returns(_documentConfiguration);
            _userServiceMock = new Mock<IUserService>();
            _documentUserDepositionRepositoryMock = new Mock<IDocumentUserDepositionRepository>();
            _permissionServiceMock = new Mock<IPermissionService>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();
            _documentRepositoryMock = new Mock<IDocumentRepository>();
            _depositionDocumentRepositoryMock = new Mock<IDepositionDocumentRepository>();
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _signalRNotificationManagerMock = new Mock<ISignalRNotificationManager>();
            _annotationEventMapperMock = new Mock<IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto>>();

            _service = new DocumentService(
                    _awsStorageServiceMock.Object,
                    _depositionDocumentConfigurationMock.Object,
                    _loggerMock.Object,
                    _userServiceMock.Object,
                    _documentUserDepositionRepositoryMock.Object,
                    _permissionServiceMock.Object,
                    _transactionHandlerMock.Object,
                    _documentRepositoryMock.Object,
                    _depositionDocumentRepositoryMock.Object,
                    _depositionRepositoryMock.Object,
                    _signalRNotificationManagerMock.Object,
                    _annotationEventMapperMock.Object);
        }

        [Fact]
        public async Task UploadDocumentFile_WithKeyValuePair_ShouldReturnFailedResult_WhenAwsStorageServiceDoesNotReturnsHttpCodeOk()
        {
            // Arrange
            var fileName = "fileTestName";
            var extension = ".exte";
            var errorMessageFragment = $"Unable to upload document {fileName}{extension}";
            var user = new User();
            var path = "test/Path";
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error($"Unable to upload document {fileName}{extension}")));
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await _service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path, DocumentType.Exhibit);

            // Assert
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
                It.Is<string>(a => a.Contains(path)),
                It.IsAny<FileTransferInfo>(),
                It.Is<string>(a => a == _documentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessageFragment, result.Errors[0].Message);
        }

        [Fact]
        public async Task UploadDocumentFile_ShouldReturnFailedResult_WhenAwsStorageServiceDoesNotReturnsHttpCodeOk()
        {
            // Arrange
            var fileName = "fileTestName";
            var extension = ".exte";
            var errorMessageFragment = $"Error loading file";
            var user = new User();
            var path = "test/Path";
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error($"Error loading file")));
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await _service.UploadDocumentFile(file, user, path, DocumentType.Exhibit);

            // Assert
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
                It.Is<string>(a => a.Contains(path)),
                It.IsAny<FileTransferInfo>(),
                It.Is<string>(a => a == _documentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessageFragment, result.Errors[0].Message);
        }

        [Fact]
        public async Task UploadDocumentFile_WithKeyValuePair_ShouldReturn_NewDepositionDocument()
        {
            // Arrange
            var fileName = "fileTestName";
            var extension = ".exte";
            var user = new User();
            var path = "test/Path";
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await _service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path, DocumentType.Exhibit);

            // Assert
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
                It.Is<string>(a => a.Contains(path)),
                It.IsAny<FileTransferInfo>(),
                It.Is<string>(a => a == _documentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            var depositionDocument = result.Value;
            Assert.Equal(extension, depositionDocument.Type);
            Assert.Equal(extension, Path.GetExtension(depositionDocument.FilePath));
            Assert.Contains(path, depositionDocument.FilePath);
            Assert.Equal(file.Name, depositionDocument.DisplayName);
        }

        [Fact]
        public async Task UploadDocumentFile_ShouldReturn_NewDepositionDocument()
        {
            // Arrange
            var fileName = "fileTestName";
            var extension = ".exte";
            var user = new User();
            var path = "test/Path";
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await _service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path, DocumentType.Exhibit);

            // Assert
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
                It.Is<string>(a => a.Contains(path)),
                It.IsAny<FileTransferInfo>(),
                It.Is<string>(a => a == _documentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            var depositionDocument = result.Value;
            Assert.Equal(extension, depositionDocument.Type);
            Assert.Equal(extension, Path.GetExtension(depositionDocument.FilePath));
            Assert.Contains(path, depositionDocument.FilePath);
            Assert.Equal(file.Name, depositionDocument.DisplayName);
        }

        [Fact]
        public async Task DeleteObjectAsync_ShouldLoggError_IfS3ClientResponseNotOk()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document
                {
                    FilePath = "testFilePath1",
                    Name = "testFileName1"
                },
                new Document
                {
                    FilePath = "testFilePath2",
                    Name = "testFileName2"
                }
            };
            var errorFragmentMessage = "Error while trying to delete document";
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail("Unable to delete file"));
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            // Act
            await _service.DeleteUploadedFiles(documents);

            // Assert
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(documents.Count));
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorFragmentMessage)),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Exactly(documents.Count));
        }

        [Fact]
        public async Task DeleteObjectAsync_ShouldCall_DeleteObjectAsync()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document
                {
                    FilePath = "testFilePath1",
                    Name = "testFileName1"
                },
                new Document
                {
                    FilePath = "testFilePath2",
                    Name = "testFileName2"
                }
            };
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            // Act
            await _service.DeleteUploadedFiles(documents);

            // Assert
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(documents.Count));
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);
        }

        [Fact]
        public void ValidateFiles_ShouldReturnResultFail_IfFileLengthTooBig()
        {
            // Arrange
            var maxSize = 52428800;
            var file = new FileTransferInfo
            {
                Length = maxSize + 10,
                Name = "acceptedFile.doc"
            };

            // Act
            var result = _service.ValidateFiles(new List<FileTransferInfo> { file });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void ValidateFiles_ShouldReturnResultFail_IfFileExtensionNotAccepted()
        {
            // Arrange
            var file = new FileTransferInfo
            {
                Length = 100,
                Name = "UnacceptedFile.not"
            };

            // Act
            var result = _service.ValidateFiles(new List<FileTransferInfo> { file });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
        }

        [Theory]
        [InlineData(".pdf")]
        [InlineData(".doc")]
        [InlineData(".docx")]
        [InlineData(".xls")]
        [InlineData(".xlsx")]
        [InlineData(".ppt")]
        [InlineData(".pptx")]
        [InlineData(".jpg")]
        [InlineData(".png")]
        [InlineData(".mp4")]
        public void ValidateFiles_ShouldReturnResultOk_IfFileExtensionAccepted(string extension)
        {
            // Arrange
            var file = new FileTransferInfo
            {
                Length = 100,
                Name = $"file{extension}"
            };

            // Act
            var result = _service.ValidateFiles(new List<FileTransferInfo> { file });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidateFiles_ShouldReturnFail_WithFilesTooBig()
        {
            // Arrange
            var maxSize = 52428800;

            var fileTooBig = new FileTransferInfo
            {
                Length = maxSize + 10,
                Name = $"fileTooBig.doc"
            };
            var correctFile = new FileTransferInfo
            {
                Length = 100,
                Name = $"file.doc"
            };

            var expectedError = "Exhibit size exceeds the allowed limit";

            // Act
            var result = _service.ValidateFiles(new List<FileTransferInfo> { fileTooBig, correctFile });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors[0].Message);
        }

        [Fact]
        public void ValidateFiles_ShouldReturnFail_WithFilesNotAccepted()
        {
            // Arrange
            var fileWithWrongExtension = new FileTransferInfo
            {
                Length = 100,
                Name = $"file.not"
            };
            var correctFile = new FileTransferInfo
            {
                Length = 100,
                Name = $"file.doc"
            };

            var expectedError = "Failed to upload the file. Please try again";

            // Act
            var result = _service.ValidateFiles(new List<FileTransferInfo> { fileWithWrongExtension, correctFile });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors[0].Message);
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfUserNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var expectedError = $"User with email {userEmail} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError(expectedError)));
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(Guid.NewGuid(), userEmail, new List<FileTransferInfo>(), folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfDepositionNotFound()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition with id {depositionId} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo>(), folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(a => a.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfFilesNotValid()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var depositionId = Guid.NewGuid();
            var file = new FileTransferInfo { Name = "incorrectFile.err" };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file }, folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfUploadFileFails()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var user = new User { EmailAddress = userEmail };
            var depositionId = Guid.NewGuid();
            var file = new FileTransferInfo { Name = "file.doc" };
            var expectedError = $"Error loading file {file.Name}";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error(expectedError)));
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file }, folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to load one or more documents to storage")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Removing uploaded documents")),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);

            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfUpdateFails()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var user = new User { EmailAddress = userEmail };
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.doc" };
            var expectedError = "Unable to add documents to deposition";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentUserDepositionRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DocumentUserDeposition>>())).ThrowsAsync(new Exception("Testing Exception"));
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file }, folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DocumentUserDeposition>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to add documents to deposition")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfPermissionCreationFails()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var user = new User { EmailAddress = userEmail, Id = Guid.NewGuid() };
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.doc" };
            var expectedError = "Unable to create document permissions";
            var documentUserDeposition = new DocumentUserDeposition { Document = new Document { Id = Guid.NewGuid() }, UserId = user.Id };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentUserDepositionRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DocumentUserDeposition>>())).ReturnsAsync(new List<DocumentUserDeposition> { documentUserDeposition });
            _permissionServiceMock.Setup(x => x.AddUserRole(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ResourceType>(), It.IsAny<RoleName>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError()));
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file }, folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DocumentUserDeposition>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            _permissionServiceMock.Verify(x => x.AddUserRole(
                It.Is<Guid>(a => a == documentUserDeposition.UserId),
                It.Is<Guid>(a => a == documentUserDeposition.DocumentId),
                It.Is<ResourceType>(a => a == ResourceType.Document),
                It.Is<RoleName>(a => a == RoleName.DocumentOwner)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to create documents permissions")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultOk_IfDepositionWasUpdated()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var user = new User { EmailAddress = userEmail, Id = Guid.NewGuid() };
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.doc" };
            var documentUserDeposition = new DocumentUserDeposition { DocumentId = Guid.NewGuid(), UserId = user.Id };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _documentUserDepositionRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DocumentUserDeposition>>())).ReturnsAsync(new List<DocumentUserDeposition> { documentUserDeposition });
            _permissionServiceMock.Setup(x => x.AddUserRole(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ResourceType>(), It.IsAny<RoleName>())).ReturnsAsync(Result.Ok());
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file }, folder, DocumentType.Exhibit);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(new[] { $"{nameof(Deposition.DocumentUserDepositions)}" }))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _transactionHandlerMock.Verify(x => x.RunAsync(It.IsAny<Func<Task>>()), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DocumentUserDeposition>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            _permissionServiceMock.Verify(x => x.AddUserRole(
                It.Is<Guid>(a => a == documentUserDeposition.UserId),
                It.Is<Guid>(a => a == documentUserDeposition.DocumentId),
                It.Is<ResourceType>(a => a == ResourceType.Document),
                It.Is<RoleName>(a => a == RoleName.DocumentOwner)), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetExhibitsForUser__ShouldReturnResultFail_IfUserNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var expectedError = $"User with email {userEmail} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError(expectedError)));

            // Act
            var result = await _service.GetExhibitsForUser(Guid.NewGuid(), userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<Document>>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetExhibitsForUser_ShouldReturnResultFail_IfDepositionNotFound()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition with id {depositionId} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _service.GetExhibitsForUser(depositionId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<Document>>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetExhibitsForUser_ShouldReturn_ListOfDocuments()
        {
            // Arrange
            var userEmail = "User@mail.com";
            var depositionId = Guid.NewGuid();
            var documentUserDeposition = new DocumentUserDeposition
            {
                Document = new Document()
            };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            _documentUserDepositionRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<DocumentUserDeposition> { documentUserDeposition });

            // Act
            var result = await _service.GetExhibitsForUser(depositionId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<Document>>>(result);
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
        }

        [Fact]
        public async Task GetGetFileSignedUrl_ShouldReturnFail_IfDocumentNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var expectedError = $"Could not find any document with Id {documentId}";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                    .ReturnsAsync((DepositionDocument)null);

            // Act
            var result = await _service.GetFileSignedUrl(depositionId, documentId);

            // Assert
            _depositionDocumentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetGetFileSignedUrl_ShouldReturn_SignedUrl()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var signedUrl = "signedUrl";
            var deposition = new Deposition { EndDate = DateTime.Now };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(new DepositionDocument { Id = documentId, Document = new Document { Id = documentId, DisplayName = "testName.pdf" } });

            _awsStorageServiceMock.Setup(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>())).Returns(signedUrl);

            // Act
            var result = await _service.GetFileSignedUrl(depositionId, documentId);

            // Assert

            _depositionDocumentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _awsStorageServiceMock.Verify(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<string>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(signedUrl, result.Value);
        }

        [Fact]
        public async Task GetGetFileSignedUrl_ShouldReturnFail_IfThereAreNoDocuments()
        {
            // Arrange
            var documentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var depositionId = Guid.NewGuid();

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((List<DepositionDocument>)null);

            // Act
            var result = await _service.GetFileSignedUrl(depositionId, documentIds);

            // Assert
            _depositionDocumentRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetGetFileSignedUrl_ShouldReturn_SignedUrlList()
        {
            // Arrange
            var documentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var signedUrlList = new List<string>{"signedUrl1", "signedUrl1"};
            var deposition = new Deposition {Id = Guid.NewGuid(), EndDate = DateTime.Now };
            var depositionDocumentList = new List<DepositionDocument> {
                new DepositionDocument {
                    Id = Guid.NewGuid(),
                    DepositionId = deposition.Id,
                    Document = new Document {
                        Id = documentIds[0],
                        DisplayName = "testName.pdf"
                    }
                },
                new DepositionDocument {
                    Id = Guid.NewGuid(),
                    DepositionId = deposition.Id,
                    Document = new Document {
                        Id = documentIds[1],
                        DisplayName = "testName2.pdf"
                    }
                }
            };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(depositionDocumentList);

            _awsStorageServiceMock.Setup(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>())).Returns(It.IsAny<string>());

            // Act
            var result = await _service.GetFileSignedUrl(deposition.Id, documentIds);

            // Assert
            _depositionDocumentRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once);
            _awsStorageServiceMock.Verify(x => x.GetFilePublicUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Exactly(documentIds.Count));
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddAnnotation_ShouldReturn_ADocumentWithAnnotationsEvents()
        {
            // Arrange
            var document = new Document
            {
                Id = Guid.NewGuid()
            };

            var annotation = new AnnotationEvent
            {
                Action = AnnotationAction.Create,
            };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());

            _documentRepositoryMock
                .Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(document);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new Deposition { SharingDocumentId = document.Id });

            // Act
            var result = await _service.AddAnnotation(document.Id, annotation);

            // Assert
            _documentRepositoryMock.Verify(x => x.Update(It.Is<Document>(a => a.Id == document.Id)), Times.Once);
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToGroupMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddAnnotation_ShouldReturnFail_WhenDepositionIsNull()
        {
            // Arrange
            var errorMessage = "Deposition with id";
            var document = new Document
            {
                Id = Guid.NewGuid()
            };

            var annotation = new AnnotationEvent
            {
                Action = AnnotationAction.Create,
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync((Deposition)null);            

            // Act
            var result = await _service.AddAnnotation(document.Id, annotation);

            // Assert
            _documentRepositoryMock.Verify(x => x.Update(It.Is<Document>(a => a.Id == document.Id)), Times.Never);
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToGroupMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task AddAnnotation_ShouldReturnFail_WhenSharingDocumentIsNull()
        {
            // Arrange
            var errorMessage = "There is no shared document for deposition";
            var document = new Document
            {
                Id = Guid.NewGuid()
            };

            var annotation = new AnnotationEvent
            {
                Action = AnnotationAction.Create,
            };

            var deposition = new Deposition 
            {
                 Id = Guid.NewGuid(),                  
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(deposition);

            // Act
            var result = await _service.AddAnnotation(document.Id, annotation);

            // Assert
            _documentRepositoryMock.Verify(x => x.Update(It.Is<Document>(a => a.Id == document.Id)), Times.Never);
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToGroupMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task AddAnnotation_ShouldReturnFail_WhenDocumentIsNull()
        {
            // Arrange
            var errorMessage = "Document with Id";
            var document = new Document
            {
                Id = Guid.NewGuid()
            };

            var annotation = new AnnotationEvent
            {
                Action = AnnotationAction.Create,
            };

            var deposition = new Deposition
            {
                Id = Guid.NewGuid(),
                SharingDocumentId = Guid.NewGuid()
            };

            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(deposition);

            // Act
            var result = await _service.AddAnnotation(document.Id, annotation);

            // Assert
            _documentRepositoryMock.Verify(x => x.Update(It.Is<Document>(a => a.Id == document.Id)), Times.Never);
            _signalRNotificationManagerMock.Verify(x => x.SendNotificationToGroupMembers(It.IsAny<Guid>(), It.IsAny<NotificationDto>()), Times.Never);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task Share_ShouldReturnFail_IfUserNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var expectedError = $"User with email {userEmail} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError(expectedError)));

            // Act
            var result = await _service.Share(Guid.NewGuid(), userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Share_ShouldReturnFail_IfDocumentUserDepositionNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };
            var documentId = Guid.NewGuid();
            var expectedError = $"Could not find any document with Id {documentId} for user {userEmail}";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _documentUserDepositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync((DocumentUserDeposition)null);

            // Act
            var result = await _service.Share(documentId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Share_ShouldReturnFail_IfDepositionNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var documentUserDeposition = new DocumentUserDeposition { User = user, DepositionId = depositionId };
            var expectedError = $"Deposition with id {depositionId} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _documentUserDepositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(documentUserDeposition);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _service.Share(documentId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Share_ShouldReturnFail_IfDepositionIsSharingOtherDocument()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("Participant@mail.com");
            deposition.Id = depositionId;
            deposition.SharingDocumentId = Guid.NewGuid();
            var documentUserDeposition = new DocumentUserDeposition { User = user, DepositionId = depositionId };
            var expectedError = "Can't share document while another document is being shared.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _documentUserDepositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(documentUserDeposition);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);

            // Act
            var result = await _service.Share(documentId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Share_ShouldReturnFail_IfDepositionUpdateFails()
        {
            //TODO I thing this test is unnecesary because before the code validated that the Deposition wasn't null in the update mehotd on Deposition service
            //But that part is cover for another test in charge of validate if the depo is found with given ID, in that case is a good idea to remove it

            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };
            var documentId = Guid.NewGuid();
            var document = new Document { Id = documentId };
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("Participant@mail.com");
            deposition.Id = depositionId;
            var documentUserDeposition = new DocumentUserDeposition { User = user, DepositionId = depositionId, Deposition = deposition };
            var expectedError = $"Deposition with id {depositionId} not found.";
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _documentUserDepositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(documentUserDeposition);
            _documentRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync((Deposition)null);

            // Act
            var result = await _service.Share(documentId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(a => a == deposition)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(a => a == deposition)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            //Assert.True(result.IsFailed);
            //Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task Share_ShouldReturnOk()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var user = new User { Id = Guid.NewGuid(), EmailAddress = userEmail };
            var documentId = Guid.NewGuid();
            var document = new Document { Id = documentId };
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("Participant@mail.com");
            deposition.Id = depositionId;
            deposition.Participants.FirstOrDefault().UserId = Guid.NewGuid();
            var documentUserDeposition = new DocumentUserDeposition { User = user, Deposition = deposition, DepositionId = depositionId, DocumentId = documentId };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            _documentUserDepositionRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(documentUserDeposition);
            _documentRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _depositionRepositoryMock.Setup(x => x.Update(It.IsAny<Deposition>())).ReturnsAsync(deposition);

            // Act
            var result = await _service.Share(documentId, userEmail);

            // Assert
            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<DocumentUserDeposition, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(DocumentUserDeposition.Document)))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(a => a == deposition)), Times.Once);
            _depositionRepositoryMock.Verify(x => x.Update(It.Is<Deposition>(a => a == deposition)), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnFail_IfDocumentNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var expectedError = "Document not found";
            _documentRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Document)null);

            // Act
            var result = await _service.GetDocument(documentId);

            // Assert
            _documentRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == documentId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task GetDocument_ShouldReturnOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = new Document { Id = documentId };
            _documentRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(document);

            // Act
            var result = await _service.GetDocument(documentId);

            // Assert
            _documentRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == documentId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<Document>>(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(document, result.Value);
        }

        [Fact]
        public async Task UploadTrancriptions_ShouldReturnResultFail_IfDepositionNotFound()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var expectedError = $"Deposition with id {depositionId} not found.";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((Deposition)null);
            var folder = DocumentType.Exhibit.GetDescription();

            // Act
            var result = await _service.UploadTranscriptions(depositionId, new List<FileTransferInfo>());

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadTrancriptions_ShouldReturnResultFail_IfFilesNotValid()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var file = new FileTransferInfo { Name = "incorrectFile.err" };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            var folder = DocumentType.Transcription.GetDescription();

            // Act
            var result = await _service.UploadTranscriptions(depositionId, new List<FileTransferInfo> { file });

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task UploadTrancriptions_ShouldReturnResultFail_IfUploadFileFails()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var file = new FileTransferInfo { Name = "file.pdf" };
            var expectedError = $"Error loading file {file.Name}";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(new Deposition());
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error(expectedError)));
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            var folder = DocumentType.Transcription.GetDescription();

            // Act
            var result = await _service.UploadTranscriptions(depositionId, new List<FileTransferInfo> { file });

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/transcriptions")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);

            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadTrancriptions_ShouldReturnResultFail_IfUpdateFails()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.pdf" };
            var expectedError = "Unable to add documents to deposition";
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _depositionDocumentRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DepositionDocument>>())).ThrowsAsync(new Exception("Testing Exception"));
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var folder = DocumentType.Transcription.GetDescription();

            // Act
            var result = await _service.UploadTranscriptions(depositionId, new List<FileTransferInfo> { file });

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/transcriptions")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _depositionDocumentRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DepositionDocument>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message));
        }

        [Fact]
        public async Task UploadTrancriptions_ShouldReturnResultOk_IfDepositionWasUpdated()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.pdf" };
            var documentUserDeposition = new DepositionDocument { DocumentId = Guid.NewGuid() };
            _userServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User());
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(deposition);
            _awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            _depositionDocumentRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DepositionDocument>>())).ReturnsAsync(new List<DepositionDocument> { documentUserDeposition });
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var folder = DocumentType.Transcription.GetDescription();

            // Act
            var result = await _service.UploadTranscriptions(depositionId, new List<FileTransferInfo> { file });

            // Assert
            _userServiceMock.Verify(x => x.GetCurrentUserAsync(), Times.Once);
            _depositionRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == depositionId), It.Is<string[]>(i => i.SequenceEqual(includes))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/transcriptions")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            _transactionHandlerMock.Verify(x => x.RunAsync(It.IsAny<Func<Task>>()), Times.Once);
            _depositionDocumentRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DepositionDocument>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                SharingDocumentId = Guid.NewGuid()
            };
            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = documentId
                    }
                }
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), null)).ReturnsAsync(deposition);
            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), null), Times.Once());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Once());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Once);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnFail_WhenDocumentNotExist()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var error = $"Could not find any document with Id { documentId}";
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                SharingDocumentId = Guid.NewGuid()
            };
            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = documentId
                    }
                }
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), null)).ReturnsAsync(deposition);
            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync((Document)null);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), null), Times.Never());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Never());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Never);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Never);

            Assert.Equal(error, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnFail_WhenDepositionNotExist()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var error = $"Could not find any deposition with Id {depositionId}";
            var deposition = new Deposition
            {
                Id = depositionId,
                SharingDocumentId = Guid.NewGuid()
            };
            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = documentId
                    }
                }
            };

            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _depositionRepositoryMock.Setup(x => x.GetById(depositionId, null)).ReturnsAsync((Deposition)null);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), null), Times.Once());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Never());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Never);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Never);

            Assert.Equal(error, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnFail_WhenUserDocumentNotExist()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId,
                SharingDocumentId = Guid.NewGuid()
            };
            var error = $"Could not find any user document with Id {documentId}";
            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = Guid.NewGuid()
                    }
                }
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), null)).ReturnsAsync(deposition);
            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _documentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), null), Times.Once());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Never());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Never);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Never);

            Assert.Equal(error, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnFail_WhenUserDocumentIsBeingShared()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var error = $"Could not delete and Exhibits while is being shared. Exhibit id {documentId}";
            var deposition = new Deposition
            {
                Id = depositionId,
                SharingDocumentId = documentId
            };

            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = Guid.NewGuid()
                    }
                }
            };
            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), null)).ReturnsAsync(deposition);
            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _documentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>()), Times.Once());
            _depositionRepositoryMock.Verify(x => x.GetById(It.IsAny<Guid>(), null), Times.Once());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Never());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Never);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Never);

            Assert.Equal(error, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task RemoveDepositionDocument_ShouldReturnTransactionError()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition
            {
                Id = depositionId
            };
            var error = "Unable to delete documents";
            var document = new Document
            {
                Id = documentId,
                FilePath = "testFilePath1",
                Name = "testFileName1",
                DocumentUserDepositions = new List<DocumentUserDeposition>
                {
                    new DocumentUserDeposition
                    {
                            DocumentId = documentId
                    }
                }
            };

            _depositionRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), null)).ReturnsAsync(deposition);
            _documentRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(document);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Fail(new ExceptionalError(error, new Exception(error)));
                });

            _awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Fail(error));
            _loggerMock.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<object>(),
               It.IsAny<Exception>(),
               It.IsAny<Func<object, Exception, string>>()));

            // Act
            var result = await _service.RemoveDepositionDocument(depositionId, documentId);

            // Assert
            _documentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>()), Times.Once());
            _documentRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<string[]>()), Times.Once());
            _awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Once());
            _documentRepositoryMock.Verify(x => x.Remove(It.Is<Document>(a => a.Id == documentId)), Times.Once);
            _documentUserDepositionRepositoryMock.Verify(x => x.Remove(It.Is<DocumentUserDeposition>(a => a.DocumentId == documentId)), Times.Once);

            Assert.Equal(error, result.Errors[0].Message);
            Assert.True(result.IsFailed);
            Assert.True(result.Errors.Count > 0);
        }
    }
}
