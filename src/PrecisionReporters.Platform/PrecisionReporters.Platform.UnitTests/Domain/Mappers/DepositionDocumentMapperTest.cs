using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DepositionDocumentMapperTest
    {
        private readonly DepositionDocumentMapper _depositionDocumentMapper;

        public DepositionDocumentMapperTest()
        {
            _depositionDocumentMapper = new DepositionDocumentMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var depoId = Guid.NewGuid();

            var model = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                DocumentId = id,
                DepositionId = depoId,
                StampLabel = "LABEL1234",
                Deposition = DepositionFactory.GetDeposition(depoId, depoId),
                Document = new Document {                
                    Id = id,
                    CreationDate = It.IsAny<DateTime>(),           
                }
            };

            // Act
            var result = _depositionDocumentMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.DocumentId, result.DocumentId);
            Assert.Equal(model.DepositionId, result.DepositionId);
            Assert.Equal(model.StampLabel, result.StampLabel);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithDepositionDocumentDto()
        {
            // Arrange
            var dto = new DepositionDocumentDto
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                DocumentId = Guid.NewGuid(),
                DepositionId = Guid.NewGuid(),
                StampLabel = "LABEL1234"
            };

            // Act
            var result = _depositionDocumentMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate.UtcDateTime, result.CreationDate);
            Assert.Equal(dto.DocumentId, result.DocumentId);
            Assert.Equal(dto.DepositionId, result.DepositionId);
            Assert.Equal(dto.StampLabel, result.StampLabel);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateDepositionDocumentDto()
        {
            // Arrange
            var dto = new CreateDepositionDocumentDto
            {
                DocumentId = Guid.NewGuid(),
                DepositionId = Guid.NewGuid(),
            };

            // Act
            var result = _depositionDocumentMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.DocumentId, result.DocumentId);
            Assert.Equal(dto.DepositionId, result.DepositionId);
        }
    }
}
