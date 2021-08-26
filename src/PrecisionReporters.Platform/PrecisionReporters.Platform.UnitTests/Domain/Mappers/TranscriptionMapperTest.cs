using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class TranscriptionMapperTest
    {
        private readonly TranscriptionMapper _transcriptionMapper;

        public TranscriptionMapperTest()
        {
            _transcriptionMapper = new TranscriptionMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithTranscriptionDto()
        {
            // Arrange
            var dto = new TranscriptionDto
            {
                DepositionId = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                Id = Guid.NewGuid(),
                PostProcessed = true,
                Text = "test1",
                TranscriptDateTime = It.IsAny<DateTime>(),
                UserEmail = "user@mock.com",
                UserId = Guid.NewGuid(),
                UserName = "testuser"
            };

            // Act
            var result = _transcriptionMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate, result.CreationDate);
            Assert.Equal(dto.Text, result.Text);
            Assert.Equal(dto.DepositionId, result.DepositionId);
            Assert.Equal(dto.UserId, result.UserId);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.TranscriptDateTime, result.TranscriptDateTime);
            Assert.Equal(dto.PostProcessed, result.PostProcessed);
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            var id = Guid.NewGuid();
            // Arrange
            var model = new Transcription
            {
                DepositionId = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                Id = Guid.NewGuid(),
                PostProcessed = true,
                Text = "test1",
                TranscriptDateTime = It.IsAny<DateTime>(),
                UserId = id,
                Confidence = 97777777777777,
                Duration = 50,
                User = UserFactory.GetUserByGivenId(id)
            };

            // Act
            var result = _transcriptionMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Text, result.Text);
            Assert.Equal(model.DepositionId, result.DepositionId);
            Assert.Equal(model.UserId, result.UserId);
            Assert.Equal(model.User?.GetFullName(), result.UserName);
            Assert.Equal(new DateTimeOffset(model.TranscriptDateTime, TimeSpan.Zero), result.TranscriptDateTime);
            Assert.Equal(model.User.EmailAddress, result.UserEmail);
            Assert.Equal(model.PostProcessed, result.PostProcessed);
        }
    }
}
