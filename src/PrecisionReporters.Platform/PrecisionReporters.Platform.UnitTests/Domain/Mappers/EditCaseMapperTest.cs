using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class EditCaseMapperTest
    {
        private readonly EditCaseMapper _editCaseMapper;

        public EditCaseMapperTest()
        {
            _editCaseMapper = new EditCaseMapper();
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithEditCaseDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new EditCaseDto
            {
                Name = "John Doe Case",
                CaseNumber = "A77"
            };

            // Act
            var result = _editCaseMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.CaseNumber, result.CaseNumber);
        }
    }
}
