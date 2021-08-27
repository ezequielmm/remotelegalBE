using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class CompositionMapperTest
    {
        private readonly CompositionMapper _compositionMapper;

        public CompositionMapperTest()
        {
            _compositionMapper = new CompositionMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var model = new Composition
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                EndDate = It.IsAny<DateTime>(),
                FileType = "pdf",
                LastUpdated = It.IsAny<DateTime>(),
                MediaUri = "mock.com",
                Room = RoomFactory.GetRoomById(id),
                RoomId = id,
                SId = "CJ12345678",
                StartDate = It.IsAny<DateTime>(),
                Status = CompositionStatus.Available,
                Url = "mock.com"
            };

            // Act
            var result = _compositionMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Status.ToString(), result.Status);
            Assert.Equal(model.StartDate, result.StartDate);
            Assert.Equal(model.EndDate, result.EndDate);
            Assert.Equal(model.LastUpdated, result.LastUpdated);
            Assert.Equal(model.SId, result.SId);
            Assert.Equal(model.Url, result.Url);
            Assert.Equal(model.MediaUri, result.MediaUrl);
            Assert.Equal(model.RoomId, result.RoomId);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCompositionDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new CompositionDto
            {
                SId = "CJ123456789",
                Url = "mock.com",
                MediaUrl = "mediamock.com"
            };

            // Act
            var result = _compositionMapper.ToModel(dto);
            Enum.TryParse(dto.Status, true, out CompositionStatus compositionStatus);


            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.SId, result.SId);
            Assert.Equal(dto.Url, result.Url);
            Assert.Equal(dto.MediaUrl, result.MediaUri);
            Assert.Equal(compositionStatus, result.Status);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCallbackCompositionDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new CallbackCompositionDto
            {
                Url = "mock.com",
                MediaUri = "mediamock.com",
                CompositionSid = "CJ123456789",
                RoomSid = "RM123456789",
                StatusCallbackEvent = "room-ended"
            };

            // Act
            var result = _compositionMapper.ToModel(dto);
            var status = dto.StatusCallbackEvent.Split("-")[1];
            Enum.TryParse(status, true, out CompositionStatus compositionStatus);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.CompositionSid, result.SId);
            Assert.Equal(dto.Url, result.Url);
            Assert.Equal(dto.MediaUri, result.MediaUri);
            Assert.Equal(compositionStatus, result.Status);
            Assert.Equal(dto.RoomSid, result.Room.SId);
        }
    }
}
