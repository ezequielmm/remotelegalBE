using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DepositionEventMapperTest
    {
        private readonly DepositionEventMapper _classUnderTest;

        public DepositionEventMapperTest()
        {
            _classUnderTest = new DepositionEventMapper();
        }

        [Fact]
        public void ToModel_ShouldThrowNotImplementedException_WithDepositionEventDto()
        {
            // Arrange
            var dto = new DepositionEventDto();
            var errorMessage = "The method or operation is not implemented";
            var exception = new Exception();

            // Act
            try
            {
                _classUnderTest.ToModel(dto);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Act and Assert
            Assert.Contains(errorMessage, exception.Message);
        }

        [Theory]
        [InlineData(EventType.EndDeposition)]
        [InlineData(EventType.OffTheRecord)]
        [InlineData(EventType.OnTheRecord)]
        [InlineData(EventType.StartDeposition)]
        public void ToModel_ShouldNormalizeFields_WithCreateDepositionEventDto(EventType eventType)
        {
            // Arrange
            var dto = GetCreateDepositionEventDtoByEventType(eventType);

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Details, result.Details);
            Assert.Equal(dto.EventType, result.EventType);
        }

        [Theory]
        [InlineData(EventType.EndDeposition)]
        [InlineData(EventType.OffTheRecord)]
        [InlineData(EventType.OnTheRecord)]
        [InlineData(EventType.StartDeposition)]
        public void ToDto_ShouldNormalizeFields_WithDepositionEvent(EventType eventType)
        {
            // Arrange
            var model = GetDepositionEventByEventType(eventType);

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Details, result.Details);
            Assert.Equal(model.EventType, result.EventType);
        }

        private DepositionEvent GetDepositionEventByEventType(EventType eventType)
        {
            return new DepositionEvent
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Details = "Details of a mock deposition event",
                EventType = eventType
            };
        }

        private CreateDepositionEventDto GetCreateDepositionEventDtoByEventType(EventType eventType)
        {
            return new CreateDepositionEventDto
            {
                Details = "Details of a mock create deposition event dto",
                EventType = eventType
            };
        }
    }
}