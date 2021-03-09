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

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class TranscriptionsServiceTests : IDisposable
    {
        private readonly TranscriptionService _transcriptionService;
        private readonly Mock<IDepositionDocumentRepository> _depositionDocumentRepositoryMock;
        private readonly Mock<ITranscriptionRepository> _transcriptionRepository;
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<ISignalRNotificationManager> _signalRNotificationManagerMock;

        public TranscriptionsServiceTests()
        {
            _transcriptionRepository = new Mock<ITranscriptionRepository>();
            _depositionDocumentRepositoryMock = new Mock<IDepositionDocumentRepository>();
            _userRepository = new Mock<IUserRepository>();
            _signalRNotificationManagerMock = new Mock<ISignalRNotificationManager>();
            _transcriptionService = new TranscriptionService(_transcriptionRepository.Object, _userRepository.Object,_depositionDocumentRepositoryMock.Object, _signalRNotificationManagerMock.Object);
        }

        [Fact]
        public async Task GetTranscriptionsFiles_ReturnTranscriptionsFilesList()
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
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(documentList);

            //  Act
            var result = await _transcriptionService.GetTranscriptionsFiles(depositionId);
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
            var documentList = new List<DepositionDocument>();

            _depositionDocumentRepositoryMock
                .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(documentList);

            //  Act
            var result = await _transcriptionService.GetTranscriptionsFiles(depositionId);
            var documentResult = result.Value;

            // Assert
            _depositionDocumentRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<DepositionDocument, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<DepositionDocument, bool>>>(), It.IsAny<string[]>()), Times.Once());
            Assert.True(documentResult.Count.Equals(0));
        }

        public void Dispose()
        {
            // Tear down
        }
    }
}
