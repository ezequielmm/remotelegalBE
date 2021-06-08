using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DocumentMapperTest
    {
        private readonly DocumentMapper _classUnderTest;

        public DocumentMapperTest()
        {
            _classUnderTest = new DocumentMapper();
        }

        [Theory]
        [InlineData(DocumentType.Caption)]
        [InlineData(DocumentType.DraftTranscription)]
        [InlineData(DocumentType.DraftTranscriptionWord)]
        [InlineData(DocumentType.Exhibit)]
        [InlineData(DocumentType.Transcription)]
        public void ToModel_ShouldNormalizeFields_WithDocumentDto(DocumentType documentType)
        {
            // Arrange
            var dto = DocumentFactory.GetDocumentDtoByDocumentType(documentType);

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.CreationDate, result.CreationDate);
            Assert.Equal(dto.DisplayName, result.DisplayName);
            Assert.Equal(dto.Size, result.Size);
            Assert.Equal(dto.AddedBy.Id, result.AddedById);
            Assert.Equal(dto.SharedAt, result.SharedAt);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateDocumentDto()
        {
            // Arrange
            var dto = DocumentFactory.GetCreateDocumentDtoByDocumentType();

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
        }

        [Fact]
        public void ToDto_ShouldNormalizeFields_WithDocument()
        {
            // Arrange
            var model = DocumentFactory.GetDocument();

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.DisplayName, result.DisplayName);
            Assert.Equal(model.Size, result.Size);
            Assert.Equal(model.AddedBy.Id, result.AddedBy.Id);
            Assert.Equal(model.AddedBy.FirstName, result.AddedBy.FirstName);
            Assert.Equal(model.AddedBy.LastName, result.AddedBy.LastName);
            Assert.Equal(model.SharedAt, result.SharedAt);
        }

        [Fact]
        public void ToDto_ShouldNormalizeFields_WithDocumentNoSharedAtDate()
        {
            // Arrange
            var model = DocumentFactory.GetDocument();
            model.SharedAt = null;

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.SharedAt);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.DisplayName, result.DisplayName);
            Assert.Equal(model.Size, result.Size);
            Assert.Equal(model.AddedBy.Id, result.AddedBy.Id);
            Assert.Equal(model.AddedBy.FirstName, result.AddedBy.FirstName);
            Assert.Equal(model.AddedBy.LastName, result.AddedBy.LastName);
        }
    }
}