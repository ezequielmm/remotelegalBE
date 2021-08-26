using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class CaseMapperTest
    {
        private readonly CaseMapper _caseMapper;

        public CaseMapperTest()
        {
            _caseMapper = new CaseMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var model = new Case
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                Name = "Case John Doe",
                CaseNumber = "A77",
                AddedById = id,
                AddedBy = UserFactory.GetUserByGivenId(id),
            };

            // Act
            var result = _caseMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.CaseNumber, result.CaseNumber);
            Assert.Equal(model.AddedById, result.AddedById);
            Assert.Equal(model.AddedBy.FirstName + " " + model.AddedBy.LastName, result.AddedBy);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCaseDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new CaseDto
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                Name = "Case John Doe",
                CaseNumber = "A77",
                AddedById = id,
                AddedBy = "John Doe",
            };

            // Act
            var result = _caseMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate.UtcDateTime, result.CreationDate);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.CaseNumber, result.CaseNumber);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateCaseDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new CreateCaseDto
            {
               CaseNumber = "A77",
               Name = "John Doe Case"
            };

            // Act
            var result = _caseMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DateTime.UtcNow, result.CreationDate, TimeSpan.FromSeconds(5));
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.CaseNumber, result.CaseNumber);
        }
    }
}
