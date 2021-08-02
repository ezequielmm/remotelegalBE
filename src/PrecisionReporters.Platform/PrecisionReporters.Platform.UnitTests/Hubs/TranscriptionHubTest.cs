using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Hubs;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Hubs
{
    public class TranscriptionHubTest
    {
        private readonly Mock<ISignalRTranscriptionFactory> _signalRTranscriptionFactoryMock;
        private readonly Mock<ILogger<TranscriptionHub>> _loggerMock;
        private readonly TranscriptionHub _classUnderTest;
        private readonly Mock<IHubContext<TranscriptionHub>> _hubContextMock;
        private readonly Mock<HubCallerContext> _clientContextMock;

        public TranscriptionHubTest()
        {
            _hubContextMock = new Mock<IHubContext<TranscriptionHub>>();
            _clientContextMock = new Mock<HubCallerContext>();
            _signalRTranscriptionFactoryMock = new Mock<ISignalRTranscriptionFactory>();
            _loggerMock = new Mock<ILogger<TranscriptionHub>>();
            _classUnderTest = new TranscriptionHub(_loggerMock.Object, _signalRTranscriptionFactoryMock.Object);
        }

        [Fact]
        public async Task SubscribeToDeposition_ResultOk_WhenUserIsAddedToGroup()
        {
            // Arrange
            var subscribeToDepositionDto = new SubscribeToDepositionDto
            {
                DepositionId = Guid.NewGuid()
            };
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;

            _hubContextMock
                .Setup(mock => mock.Groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);
            _classUnderTest.Groups = _hubContextMock.Object.Groups;

            // Act
            var result = await _classUnderTest.SubscribeToDeposition(subscribeToDepositionDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            _hubContextMock.Verify(mock => mock.Groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task SubscribeToDeposition_ResultFail_WhenUserIsNotAddedToGroup()
        {
            // Arrange
            var subscribeToDepositionDto = new SubscribeToDepositionDto
            {
                DepositionId = Guid.NewGuid()
            };

            var logErrorMessage = $"There was an error subscribing to Deposition {subscribeToDepositionDto.DepositionId}";

            // Act
            var result = await _classUnderTest.SubscribeToDeposition(subscribeToDepositionDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _hubContextMock.Verify(mock => mock.Groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task UploadTranscription_ShouldNotFail()
        {
            // Arrange
            var transcriptionsHubDto = new TranscriptionsHubDto
            {
                Audio = new byte[0],
                DepositionId = Guid.NewGuid()
            };
            var transcriptionServiceMock = new Mock<ITranscriptionLiveService>();
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()))
                .Returns(transcriptionServiceMock.Object);
            transcriptionServiceMock
                .Setup(mock => mock.RecognizeAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _classUnderTest.UploadTranscription(transcriptionsHubDto);

            // Assert
            _signalRTranscriptionFactoryMock.Verify(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()), Times.Once);
            transcriptionServiceMock.Verify(mock => mock.RecognizeAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public async Task UploadTranscription_ShouldFail()
        {
            // Arrange
            var transcriptionsHubDto = new TranscriptionsHubDto
            {
                Audio = new byte[0],
                DepositionId = Guid.NewGuid()
            };
            var transcriptionServiceMock = new Mock<ITranscriptionLiveService>();
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()))
                .Throws(new NullReferenceException());
            var logErrorMessage =
                $"There was an error uploading transcription of connectionId {_classUnderTest.Context.ConnectionId} on Deposition {transcriptionsHubDto.DepositionId}";

            // act
            Task Result()
            {
                return _classUnderTest.UploadTranscription(transcriptionsHubDto);
            }

            // Assert
            _ = await Assert.ThrowsAsync<NullReferenceException>(Result);
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()), Times.Once);
            transcriptionServiceMock.Verify(mock => mock.RecognizeAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task ChangeTranscriptionStatus_ShouldNotFail_IfIsOffRecordTrue()
        {
            // Arrange
            var transcriptionsChangeStatusDto = new TranscriptionsChangeStatusDto
            {
                DepositionId = Guid.NewGuid(),
                OffRecord = true,
                SampleRate = 16000
            };
            var userIdentifier = "mock@mail.com";
            var logMessage = $"Going OFF Record: transcriptions of {userIdentifier} on deposition {transcriptionsChangeStatusDto.DepositionId}";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.Unsubscribe(It.IsAny<string>()));

            // Act
            await _classUnderTest.ChangeTranscriptionStatus(transcriptionsChangeStatusDto);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessage),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangeTranscriptionStatus_ShouldNotFail_IfIsOffRecordFalse()
        {
            // Arrange
            var transcriptionsChangeStatusDto = new TranscriptionsChangeStatusDto
            {
                DepositionId = Guid.NewGuid(),
                OffRecord = false,
                SampleRate = 16000
            };
            var userIdentifier = "mock@mail.com";
            var logMessage = $"Going ON Record: transcriptions of {userIdentifier} on deposition {transcriptionsChangeStatusDto.DepositionId}";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));

            // Act
            await _classUnderTest.ChangeTranscriptionStatus(transcriptionsChangeStatusDto);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessage),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(
                mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangeTranscriptionStatus_ShouldFail()
        {
            // Arrange
            var transcriptionsChangeStatusDto = new TranscriptionsChangeStatusDto
            {
                DepositionId = Guid.NewGuid(),
                OffRecord = false,
                SampleRate = 16000
            };
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws(new Exception());
            var logMessage = "There was an error uploading transcription status of connectionId";

                // Act
            Task Result()
            {
                return _classUnderTest.ChangeTranscriptionStatus(transcriptionsChangeStatusDto);
            }

            // Assert
            _ = await Assert.ThrowsAsync<Exception>(Result);
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(logMessage)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(
                mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeRecognition_ShouldInitializeNewRecognition()
        {
            // Arrange
            var initializeRecognitionDto = new InitializeRecognitionDto
            {
                DepositionId = Guid.NewGuid(),
                SampleRate = 16000
            };
            var transcriptionServiceMock = new Mock<ITranscriptionLiveService>();
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()))
                .Returns(transcriptionServiceMock.Object);
            var logMessageInfo =
                $"Removing transcription service for user {userIdentifier} on deposition {initializeRecognitionDto.DepositionId}. Service already exist.";
            var logMessageFinish =
                $"Initializing transcription service for user {userIdentifier} on deposition {initializeRecognitionDto.DepositionId} with sample rate {initializeRecognitionDto.SampleRate}";
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _classUnderTest.InitializeRecognition(initializeRecognitionDto);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessageInfo || v.ToString() == logMessageFinish),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Exactly(2));
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()), Times.Once);
            _signalRTranscriptionFactoryMock.Verify(
                mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeRecognition_ShouldRemoveOldConectionAndInitializeNewRecognition()
        {
            // Arrange
            var initializeRecognitionDto = new InitializeRecognitionDto
            {
                DepositionId = Guid.NewGuid(),
                SampleRate = 16000
            };
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()))
                .Returns((TranscriptionLiveAzureService) null);
            var logMessageFinish =
                $"Initializing transcription service for user {userIdentifier} on deposition {initializeRecognitionDto.DepositionId} with sample rate {initializeRecognitionDto.SampleRate}";
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _classUnderTest.InitializeRecognition(initializeRecognitionDto);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessageFinish),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Never);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.GetTranscriptionLiveService(It.IsAny<string>()), Times.Once);
            _signalRTranscriptionFactoryMock.Verify(
                mock => mock.TryInitializeRecognition(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldUnsubscribeConnection()
        {
            // Arrange
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            var logMessageInfo = $"OnDisconnectedAsync {_classUnderTest.Context.ConnectionId} user: {userIdentifier}";

            // Act
            await _classUnderTest.OnDisconnectedAsync(null);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessageInfo),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldUnsubscribeConnection_WhenExceptionHappens()
        {
            // Arrange
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            var logMessageInfo = $"OnDisconnectedAsync {_classUnderTest.Context.ConnectionId} user: {userIdentifier}";

            // Act
            await _classUnderTest.OnDisconnectedAsync(new Exception());

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessageInfo),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception OnDisconnectedAsync")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ShouldFail()
        {
            // Arrange
            var userIdentifier = "mock@mail.com";
            _clientContextMock
                .Setup(mock => mock.UserIdentifier)
                .Returns(userIdentifier);
            _clientContextMock
                .Setup(mock => mock.ConnectionId)
                .Returns(Guid.NewGuid().ToString);
            _classUnderTest.Context = _clientContextMock.Object;
            var logMessageInfo = $"OnDisconnectedAsync {_classUnderTest.Context.ConnectionId} user: {userIdentifier}";
            _signalRTranscriptionFactoryMock
                .Setup(mock => mock.Unsubscribe(It.IsAny<string>()))
                .Throws(new Exception());
            var logMessageError = "There was an error when unsubscribing user";

            // Act
            await _classUnderTest.OnDisconnectedAsync(null);

            // Assert
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logMessageInfo),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(logMessageError)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _signalRTranscriptionFactoryMock.Verify(mock => mock.Unsubscribe(It.IsAny<string>()), Times.Once);
        }
    }
}