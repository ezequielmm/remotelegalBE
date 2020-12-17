using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DocumentServiceTest
    {
        private readonly DocumentConfiguration _documentConfiguration;

        public DocumentServiceTest()
        {
            _documentConfiguration = new DocumentConfiguration
            {
                BucketName = "testBucket",
                MaxFileSize = 52428800,
                AcceptedFileExtensions = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".png", ".mp4" }
            };
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error($"Unable to upload document {fileName}{extension}")));
            var service = InitializeService(awsStorageService: awsStorageServiceMock);
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path);

            // Assert
            awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error($"Error loading file")));
            var service = InitializeService(awsStorageService: awsStorageServiceMock);
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await service.UploadDocumentFile(file, user, path);

            // Assert
            awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            var service = InitializeService(awsStorageService: awsStorageServiceMock);
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path);

            // Assert
            awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            var service = InitializeService(awsStorageService: awsStorageServiceMock);
            await using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("testStream"));
            var file = new FileTransferInfo
            {
                FileStream = testStream,
                Name = fileName + extension,
                Length = testStream.Length,
            };

            //Act
            var result = await service.UploadDocumentFile(new KeyValuePair<string, FileTransferInfo>("fileKey", file), user, path);

            // Assert
            awsStorageServiceMock.Verify(x => x.UploadMultipartAsync(
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail("Unable to delete file"));
            var loggerMock = new Mock<ILogger<DocumentService>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var service = InitializeService(awsStorageServiceMock, loggerMock);

            // Act
            await service.DeleteUploadedFiles(documents);

            // Assert
            awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(documents.Count));
            loggerMock.Verify(x => x.Log(
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
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            var loggerMock = new Mock<ILogger<DocumentService>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var service = InitializeService(awsStorageServiceMock, loggerMock);

            // Act
            await service.DeleteUploadedFiles(documents);

            // Assert
            awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _documentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(documents.Count));
            loggerMock.Verify(x => x.Log(
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

            var service = InitializeService();

            // Act
            var result = service.ValidateFiles(new List<FileTransferInfo> { file });

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

            var service = InitializeService();

            // Act
            var result = service.ValidateFiles(new List<FileTransferInfo> { file });

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

            var service = InitializeService();

            // Act
            var result = service.ValidateFiles(new List<FileTransferInfo> { file });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidateFiles_ShouldReturnFail_WithAllTheNamesOfRejectedFiles()
        {
            // Arrange
            var maxSize = 52428800;

            var fileWithWrongExtension = new FileTransferInfo
            {
                Length = 100,
                Name = $"file.not"
            };
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

            var service = InitializeService();
            // Act
            var result = service.ValidateFiles(new List<FileTransferInfo> { fileWithWrongExtension, fileTooBig, correctFile });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(fileWithWrongExtension.Name, result.Errors[0].Message);
            Assert.Contains(fileTooBig.Name, result.Errors[0].Message);
            Assert.DoesNotContain(correctFile.Name, result.Errors[0].Message);
        }

        [Fact]
        public async Task UploadDocuments_ShouldReturnResultFail_IfUserNotFound()
        {
            // Arrange
            var userEmail = "notExisitingUser@mail.com";
            var expectedError = $"User with email {userEmail} not found.";
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Fail(new ResourceNotFoundError(expectedError)));

            var service = InitializeService(userService: userServiceMock);

            // Act
            var result = await service.UploadDocuments(Guid.NewGuid(), userEmail, new List<FileTransferInfo>());

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
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
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            var depositionServiceyMock = new Mock<IDepositionService>();
            depositionServiceyMock.Setup(x => x.GetDepositionByIdWithDocumentUsers(It.IsAny<Guid>())).ReturnsAsync(Result.Fail(new Error(expectedError)));

            var service = InitializeService(userService: userServiceMock, depositionService: depositionServiceyMock);

            // Act
            var result = await service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo>());

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            depositionServiceyMock.Verify(x => x.GetDepositionByIdWithDocumentUsers(It.Is<Guid>(a => a == depositionId)), Times.Once);
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
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(new User()));
            var depositionServiceyMock = new Mock<IDepositionService>();
            depositionServiceyMock.Setup(x => x.GetDepositionByIdWithDocumentUsers(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(new Deposition()));

            var service = InitializeService(userService: userServiceMock, depositionService: depositionServiceyMock);

            // Act
            var result = await service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file });

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            depositionServiceyMock.Verify(x => x.GetDepositionByIdWithDocumentUsers(It.Is<Guid>(a => a == depositionId)), Times.Once);
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
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            var depositionServiceyMock = new Mock<IDepositionService>();
            depositionServiceyMock.Setup(x => x.GetDepositionByIdWithDocumentUsers(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(new Deposition()));
            var awsStrogaeServiceMock = new Mock<IAwsStorageService>();
            awsStrogaeServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error(expectedError)));
            awsStrogaeServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var service = InitializeService(userService: userServiceMock, depositionService: depositionServiceyMock, awsStorageService: awsStrogaeServiceMock, logger: loggerMock);

            // Act
            var result = await service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file });

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            depositionServiceyMock.Verify(x => x.GetDepositionByIdWithDocumentUsers(It.Is<Guid>(a => a == depositionId)), Times.Once);
            awsStrogaeServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            loggerMock.Verify(x => x.Log(
               It.Is<LogLevel>(l => l == LogLevel.Error),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to load one or more documents to storage")),
               It.IsAny<Exception>(),
               It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            loggerMock.Verify(x => x.Log(
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
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            var depositionServiceyMock = new Mock<IDepositionService>();
            depositionServiceyMock.Setup(x => x.GetDepositionByIdWithDocumentUsers(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(deposition));
            var awsStrogaeServiceMock = new Mock<IAwsStorageService>();
            awsStrogaeServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            awsStrogaeServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            var documentUserDepositionRepositoryMock = new Mock<IDocumentUserDepositionRepository>();
            documentUserDepositionRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DocumentUserDeposition>>())).ThrowsAsync(new Exception("Testing Exception"));
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var service = InitializeService(userService: userServiceMock, depositionService: depositionServiceyMock, awsStorageService: awsStrogaeServiceMock, documentUserDepositionRepository: documentUserDepositionRepositoryMock, logger: loggerMock); ;

            // Act
            var result = await service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file });

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            depositionServiceyMock.Verify(x => x.GetDepositionByIdWithDocumentUsers(It.Is<Guid>(a => a == depositionId)), Times.Once);
            awsStrogaeServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            documentUserDepositionRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DocumentUserDeposition>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            loggerMock.Verify(x => x.Log(
               It.Is<LogLevel>(l => l == LogLevel.Error),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to add documents to deposition")),
               It.IsAny<Exception>(),
               It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            awsStrogaeServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
            var user = new User { EmailAddress = userEmail };
            var depositionId = Guid.NewGuid();
            var deposition = new Deposition { Id = depositionId, Documents = new List<DepositionDocument>() };
            var file = new FileTransferInfo { Name = "file.doc" };
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(Result.Ok(user));
            var depositionServiceyMock = new Mock<IDepositionService>();
            depositionServiceyMock.Setup(x => x.GetDepositionByIdWithDocumentUsers(It.IsAny<Guid>())).ReturnsAsync(Result.Ok(deposition));
            var awsStrogaeServiceMock = new Mock<IAwsStorageService>();
            awsStrogaeServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>())).ReturnsAsync(Result.Ok());
            var documentUserDepositionRepositoryMock = new Mock<IDocumentUserDepositionRepository>();
            documentUserDepositionRepositoryMock.Setup(x => x.CreateRange(It.IsAny<List<DocumentUserDeposition>>())).ReturnsAsync(new List<DocumentUserDeposition> { new DocumentUserDeposition() });
            var loggerMock = new Mock<ILogger<DocumentService>>();

            var service = InitializeService(userService: userServiceMock, depositionService: depositionServiceyMock, awsStorageService: awsStrogaeServiceMock, documentUserDepositionRepository: documentUserDepositionRepositoryMock, logger: loggerMock); ;

            // Act
            var result = await service.UploadDocuments(depositionId, userEmail, new List<FileTransferInfo> { file });

            // Assert
            userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            depositionServiceyMock.Verify(x => x.GetDepositionByIdWithDocumentUsers(It.Is<Guid>(a => a == depositionId)), Times.Once);
            awsStrogaeServiceMock.Verify(x => x.UploadMultipartAsync(It.Is<string>(a => a.Contains("/exhibits")), It.Is<FileTransferInfo>(a => a == file), It.IsAny<string>()), Times.Once);
            documentUserDepositionRepositoryMock.Verify(x => x.CreateRange(It.Is<List<DocumentUserDeposition>>(a => a.Any(d => d.Deposition == deposition))), Times.Once);
            loggerMock.Verify(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
            awsStrogaeServiceMock.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.NotNull(result);
            Assert.IsType<Result>(result);
            Assert.True(result.IsSuccess);
        }

        private DocumentService InitializeService(
            Mock<IAwsStorageService> awsStorageService = null,
            Mock<ILogger<DocumentService>> logger = null,
            Mock<IUserService> userService = null,
            Mock<IDepositionService> depositionService = null,
            Mock<IDocumentUserDepositionRepository> documentUserDepositionRepository = null)
        {
            var awsStorageServiceMock = awsStorageService ?? new Mock<IAwsStorageService>();
            var loggerMock = logger ?? new Mock<ILogger<DocumentService>>();
            var depositionDocumentConfigurationMock = new Mock<IOptions<DocumentConfiguration>>();
            depositionDocumentConfigurationMock.Setup(x => x.Value).Returns(_documentConfiguration);
            var userServiceMock = userService ?? new Mock<IUserService>();
            var depositionServiceMock = depositionService ?? new Mock<IDepositionService>();
            var documentUserDepositionRepositoryMock = documentUserDepositionRepository ?? new Mock<IDocumentUserDepositionRepository>();

            return new DocumentService(awsStorageServiceMock.Object, depositionDocumentConfigurationMock.Object, loggerMock.Object, userServiceMock.Object, depositionServiceMock.Object, documentUserDepositionRepositoryMock.Object);

        }
    }
}
