using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DocumentWithSignedUrlMapperTest
    {
        private readonly DocumentWithSignedUrlMapper _documentWithSignedUrlMapper;

        public DocumentWithSignedUrlMapperTest()
        {
            _documentWithSignedUrlMapper = new DocumentWithSignedUrlMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var model = DocumentFactory.GetDocument();

            // Act
            var result = _documentWithSignedUrlMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.Size, result.Size);
            Assert.Equal(model.DisplayName, result.DisplayName);
            Assert.Equal(model.AddedBy.FirstName + " " + model.AddedBy.LastName, result.AddedBy.FirstName + " " + result.AddedBy.LastName);
        }
    }
}
