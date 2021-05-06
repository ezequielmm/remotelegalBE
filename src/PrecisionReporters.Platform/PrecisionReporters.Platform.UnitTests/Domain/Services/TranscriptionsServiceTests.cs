using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using PrecisionReporters.Platform.UnitTests.Utils;
using FluentResults;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class TranscriptionsServiceTests : IDisposable
    {
        private readonly TranscriptionService _transcriptionService;
        private readonly Mock<IDepositionDocumentRepository> _depositionDocumentRepositoryMock;
        private readonly Mock<ITranscriptionRepository> _transcriptionRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IParticipantRepository> _participantRepositoryMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<ICompositionService> _compositionServiceMock;
        private readonly Mock<IMapper<Transcription, TranscriptionDto, object>> _transcriptionMapperMock;

        public TranscriptionsServiceTests()
        {
            _transcriptionRepositoryMock = new Mock<ITranscriptionRepository>();
            _depositionDocumentRepositoryMock = new Mock<IDepositionDocumentRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _participantRepositoryMock = new Mock<IParticipantRepository>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _compositionServiceMock = new Mock<ICompositionService>();
            _transcriptionService = new TranscriptionService(_transcriptionRepositoryMock.Object,
                _userRepositoryMock.Object,
                _depositionDocumentRepositoryMock.Object,
                _participantRepositoryMock.Object,
                _depositionServiceMock.Object,
                _compositionServiceMock.Object);
        }

        [Fact]
        public async Task GetTranscriptionsFiles_ReturnTranscriptionsFilesList()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var identity = "user@email.com";
            var documentList = new List<DepositionDocument>
            {
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName1.pdf" }, StampLabel = "Stamped1" },
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName2.pdf" }, StampLabel = "Stamped2" },
                new DepositionDocument { Id = Guid.NewGuid(), Document = new Document { Id = Guid.NewGuid(), Name = "docName3.pdf" }, StampLabel = "Stamped3" },
            };

            var participant = new Participant
            {
                UserId = Guid.NewGuid(),
                Role = ParticipantType.CourtReporter
            };

            var user = new User
            {
                FirstName = "UserName",
                LastName = "LastName",
                EmailAddress = "email@email.com"
            };

            _userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(user);

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(documentList);

            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                    .ReturnsAsync(participant);

            //  Act
            var result = await _transcriptionService.GetTranscriptionsFiles(depositionId, identity);
            var documentResult = result.Value;
            // Assert
            _depositionDocumentRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.NotNull(documentResult);
        }

        [Fact]
        public async Task GetTranscriptionsFiles_ReturnEmptyList()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var identity = "user@email.com";
            var documentList = new List<DepositionDocument>();
            var participant = new Participant
            {
                UserId = Guid.NewGuid(),
                Role = ParticipantType.CourtReporter
            };
            var user = new User
            {
                FirstName = "UserName",
                LastName = "LastName",
                EmailAddress = "email@email.com"
            };

            _userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(user);

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(documentList);

            _participantRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Participant, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                    .ReturnsAsync(participant);

            //  Act
            var result = await _transcriptionService.GetTranscriptionsFiles(depositionId, identity);
            var documentResult = result.Value;

            // Assert
            _depositionDocumentRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.True(documentResult.Count.Equals(0));
        }

        [Fact]
        public async Task GetTranscriptionsWithTimeOffset_Test_WithoutOffTheRecord()
        {
            var interval = new List<CompositionInterval>
            {
                new CompositionInterval
                {
                    Start = 5,
                    Stop = 90
                }
            };
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, Guid.NewGuid());
            deposition.Room.RecordingStartDate = DateTime.UtcNow;
            deposition.Events = DepositionFactory.GetDepositionEvents();

            _depositionServiceMock.Setup(x => x.GetByIdWithIncludes(
                It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(Result.Ok(deposition));

            _transcriptionRepositoryMock.Setup(x => x.GetByFilter(
                It.IsAny<Expression<Func<Transcription, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Transcription, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(GetTranscriptions(depositionId));

            _compositionServiceMock.Setup(x => x.GetDepositionRecordingIntervals(
                It.IsAny<List<DepositionEvent>>(),
                It.IsAny<long>())).Returns(interval);

            var result = await _transcriptionService.GetTranscriptionsWithTimeOffset(depositionId);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value[0].TranscriptionVideoTime == 1);
            Assert.True(result.Value[1].TranscriptionVideoTime == 10);
            Assert.True(result.Value[2].TranscriptionVideoTime == 60);
            Assert.True(result.Value[3].TranscriptionVideoTime == 65);
        }

        [Fact]
        public async Task GetTranscriptionsWithTimeOffset_Test_WithOffTheRecordIntervals()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, Guid.NewGuid());
            deposition.Room.RecordingStartDate = DateTime.UtcNow;

            deposition.Events = DepositionFactory.GetDepositionEvents();

            _depositionServiceMock.Setup(x => x.GetByIdWithIncludes(
                It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(Result.Ok(deposition));

            _transcriptionRepositoryMock.Setup(x => x.GetByFilter(
                It.IsAny<Expression<Func<Transcription, object>>>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<Transcription, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(GetTranscriptions(depositionId));

            _compositionServiceMock.Setup(x => x.GetDepositionRecordingIntervals(
                It.IsAny<List<DepositionEvent>>(),
                It.IsAny<long>())).Returns(GetCompositionIntervals());

            // Act
            var result = await _transcriptionService.GetTranscriptionsWithTimeOffset(depositionId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value[0].TranscriptionVideoTime == 1);
            Assert.True(result.Value[1].TranscriptionVideoTime == 10);
            Assert.True(result.Value[2].TranscriptionVideoTime == 30);
            Assert.True(result.Value[3].TranscriptionVideoTime == 35);
        }

        [Fact]
        public async Task GetTranscriptionsWithTimeOffset_ReturnFail_WhenDepositionResultIsFailed()
        {
            // Arrange
            var depositionId = Guid.NewGuid();

            _depositionServiceMock.Setup(x => x.GetByIdWithIncludes(
                It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(Result.Fail("Fail"));

            // Act
            var result = await _transcriptionService.GetTranscriptionsWithTimeOffset(depositionId);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task StoreTranscription_ReturnFail_WhenUserIsNotValid()
        {
            // Arrange
            var errorMessage = "User with such email address was not found.";

            _userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _transcriptionService.StoreTranscription(It.IsAny<Transcription>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task StoreTranscription_ReturnFail_WhenTranscriptionCreateIsFailed()
        {
            // Arrange
            var errorMessage = "Fail to create new transcription.";
            var user = new User
            {
                FirstName = "UserName",
                LastName = "LastName",
                EmailAddress = "email@email.com"
            };

            var transcription = new Transcription { Text = "transcript" };
            var depositionId = Guid.NewGuid().ToString();

            _userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(user);

            _transcriptionRepositoryMock.Setup(x => x.Create(transcription)).ReturnsAsync((Transcription)null);

            // Act
            var result = await _transcriptionService.StoreTranscription(transcription, depositionId, user.EmailAddress);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Equal(errorMessage, result.Errors[0].Message);
        }

        [Fact]
        public async Task StoreTranscription_ReturnNewTranscription_ResultOk()
        {
            // Arrange
            var user = new User
            {
                FirstName = "UserName",
                LastName = "LastName",
                EmailAddress = "email@email.com"
            };

            var transcription = new Transcription { Text = "transcript", DepositionId = Guid.NewGuid() };
            var depositionId = Guid.NewGuid().ToString();
            var transcriptionDto = new TranscriptionDto
            {
                Text = "transcript",
                DepositionId = transcription.DepositionId
            };
            var notificationDto = new NotificationDto
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.Transcript,
                Content = transcriptionDto
            };

            _userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(user);

            _transcriptionRepositoryMock.Setup(x => x.Create(transcription)).ReturnsAsync(transcription);

            // Act
            var result = await _transcriptionService.StoreTranscription(transcription, depositionId, user.EmailAddress);

            // Assert
            Assert.True(result.IsSuccess);
        }

        private List<CompositionInterval> GetCompositionIntervals()
        {
            return new List<CompositionInterval>
            {
                new CompositionInterval
                {
                    Start = 5,
                    Stop = 30
                },
                new CompositionInterval
                {
                    Start = 60,
                    Stop = 80
                }
            };
        }

        private List<Transcription> GetTranscriptions(Guid depositionId)
        {
            return new List<Transcription>()
            {
                new Transcription
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Text = "Test n1",
                    DepositionId = depositionId,
                    TranscriptDateTime = DateTime.UtcNow.AddSeconds(6),
                    User = new User
                    {
                        FirstName = "Foo1",
                        LastName = "Bar1"
                    },
                    Duration = 5,
                    Confidence = 0.8
                },
                new Transcription
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow.AddSeconds(15),
                    Text = "Text n2",
                    DepositionId = depositionId,
                    TranscriptDateTime = DateTime.UtcNow.AddSeconds(15),
                    User = new User
                    {
                        FirstName = "Foo2",
                        LastName = "Bar2"
                    },
                    Duration = 7,
                    Confidence = 0.7
                },
                new Transcription
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow.AddSeconds(65),
                    Text = "Text n3",
                    DepositionId = depositionId,
                    TranscriptDateTime = DateTime.UtcNow.AddSeconds(65),
                    User = new User
                    {
                        FirstName = "Foo3",
                        LastName = "Bar3"
                    },
                    Duration = 10,
                    Confidence = 0.7
                },
                new Transcription
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow.AddSeconds(70),
                    Text = "Text n3",
                    DepositionId = depositionId,
                    TranscriptDateTime = DateTime.UtcNow.AddSeconds(70),
                    User = new User
                    {
                        FirstName = "Foo3",
                        LastName = "Bar3"
                    },
                    Duration = 10,
                    Confidence = 0.7
                }
            };
        }

        public void Dispose()
        {
            // Tear down
        }
    }
}
