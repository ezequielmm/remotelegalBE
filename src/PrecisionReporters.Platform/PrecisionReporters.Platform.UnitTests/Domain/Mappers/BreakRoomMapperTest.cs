using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class BreakRoomMapperTest
    {
        private readonly BreakRoomMapper _breakRoomMapper;

        public BreakRoomMapperTest()
        {
            _breakRoomMapper = new BreakRoomMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var user = UserFactory.GetUserByGivenId(id);

            var model = new BreakRoom
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                Name = "BREAKROOM 1",
                IsLocked = false,
                Attendees = new List<BreakRoomAttendee>() {
                    new BreakRoomAttendee {
                        User = user
                    }
                }
            };

            // Act
            var result = _breakRoomMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.Name, result.Name);
            Assert.Equal(model.IsLocked, result.IsLocked);
            Assert.Contains(model.Attendees, p => p.User == user);
        }
    }
}
