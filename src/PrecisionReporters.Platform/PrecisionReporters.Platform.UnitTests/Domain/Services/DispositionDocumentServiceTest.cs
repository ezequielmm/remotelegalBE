using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class DispositionDocumentServiceTest
    {
        private readonly DepositionDocumentConfiguration _depositionDocumentConfiguration;

        public DispositionDocumentServiceTest()
        {
            _depositionDocumentConfiguration = new DepositionDocumentConfiguration
            {
                BucketName = "testBucket"
            };
        }

        [Fact]
        public async Task UploadDocumentFile_ShouldReturnFailedResult_WhenAwsStorageServiceDoesNotReturnsHttpCodeOk()
        {
            // Arrange
            var fileName = "fileTestName";
            var extension = ".exte";
            var errorMessageFragment = $"Unable to upload document {fileName}{extension}";
            var user = new User();
            var path = "test/Path";
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.UploadMultipartAsync(It.IsAny<string>(), It.IsAny<FileTransferInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error($"Unable to upload document")));
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
                It.Is<string>(a => a == _depositionDocumentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<DepositionDocument>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(errorMessageFragment, result.Errors[0].Message);
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
                It.Is<string>(a => a == _depositionDocumentConfiguration.BucketName)
                ), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<DepositionDocument>>(result);
            Assert.True(result.IsSuccess);
            var depositionDocument = result.Value;
            Assert.Equal(extension, depositionDocument.Type);
            Assert.Equal(extension, Path.GetExtension(depositionDocument.FilePath));
            Assert.Contains(path, depositionDocument.FilePath);
        }

        [Fact]
        public async Task DeleteObjectAsync_ShouldLoggError_IfS3ClientResponseNotOk()
        {

            var depositionDocuments = new List<DepositionDocument>
            {
                new DepositionDocument
                {
                    FilePath = "testFilePath1",
                    Name = "testFileName1"
                },
                new DepositionDocument
                {
                    FilePath = "testFilePath2",
                    Name = "testFileName2"
                }
            };
            var errorFragmentMessage = "Error while trying to delete document";
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail("Unable to delete file"));
            var loggerMock = new Mock<ILogger<DepositionDocumentService>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var service = InitializeService(awsStorageServiceMock, loggerMock);

            // Act
            await service.DeleteUploadedFiles(depositionDocuments);

            // Assert
            awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _depositionDocumentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(depositionDocuments.Count));
            loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorFragmentMessage)),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Exactly(depositionDocuments.Count));
        }

        [Fact]
        public async Task DeleteObjectAsync_ShouldCall_DeleteObjectAsync()
        {

            var depositionDocuments = new List<DepositionDocument>
            {
                new DepositionDocument
                {
                    FilePath = "testFilePath1",
                    Name = "testFileName1"
                },
                new DepositionDocument
                {
                    FilePath = "testFilePath2",
                    Name = "testFileName2"
                }
            };
            var awsStorageServiceMock = new Mock<IAwsStorageService>();
            awsStorageServiceMock.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());
            var loggerMock = new Mock<ILogger<DepositionDocumentService>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var service = InitializeService(awsStorageServiceMock, loggerMock);

            // Act
            await service.DeleteUploadedFiles(depositionDocuments);

            // Assert
            awsStorageServiceMock.Verify(x => x.DeleteObjectAsync(It.Is<string>(a => a == _depositionDocumentConfiguration.BucketName), It.IsAny<string>()), Times.Exactly(depositionDocuments.Count));
            loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);
        }

        private DepositionDocumentService InitializeService(Mock<IAwsStorageService> awsStorageService = null, Mock<ILogger<DepositionDocumentService>> logger = null)
        {
            var awsStorageServiceMock = awsStorageService ?? new Mock<IAwsStorageService>();
            var loggerMock = logger ?? new Mock<ILogger<DepositionDocumentService>>();
            var depositionDocumentConfigurationMock = new Mock<IOptions<DepositionDocumentConfiguration>>();
            depositionDocumentConfigurationMock.Setup(x => x.Value).Returns(_depositionDocumentConfiguration);

            return new DepositionDocumentService(awsStorageServiceMock.Object, depositionDocumentConfigurationMock.Object, loggerMock.Object);

        }
    }
}
