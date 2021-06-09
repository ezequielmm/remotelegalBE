using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class RoomMapperTest
    {
        private readonly RoomMapper _classUnderTest;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;

        public RoomMapperTest()
        {
            _compositionMapper = new CompositionMapper();
            _classUnderTest = new RoomMapper(_compositionMapper);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithRoomDto()
        {
            // Arrange
            var dto = RoomFactory.GetRoomDto();

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.IsRecordingEnabled, result.IsRecordingEnabled);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateRoomDto()
        {
            // Arrange
            var dto = RoomFactory.GetCreateRoomDto();

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.IsRecordingEnabled, result.IsRecordingEnabled);
        }

        [Fact]
        public void ToDto_ShouldNormalizeFields_WithRoom()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var model = RoomFactory.GetRoomById(roomId);

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.EndDate, result.EndDate);
            Assert.Equal(model.IsRecordingEnabled, result.IsRecordingEnabled);
            Assert.Equal(model.Composition.RoomId, result.Composition.RoomId);
            Assert.Equal(model.Status.ToString(), result.Status);
            Assert.Equal(model.SId, result.SId);
        }

        [Fact]
        public void ToDto_ShouldNormalizeFields_WithRoomNoComposition()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var model = RoomFactory.GetRoomById(roomId);
            model.Composition = null;

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Composition);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.EndDate, result.EndDate);
            Assert.Equal(model.IsRecordingEnabled, result.IsRecordingEnabled);
            Assert.Equal(model.Status.ToString(), result.Status);
            Assert.Equal(model.SId, result.SId);
        }
    }
}