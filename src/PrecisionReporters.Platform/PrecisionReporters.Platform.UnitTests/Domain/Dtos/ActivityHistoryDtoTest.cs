using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class ActivityHistoryDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<ActivityHistoryDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Id = It.IsAny<Guid>();
            obj.ActivityDate = It.IsAny<DateTime>();
            obj.Device = It.IsAny<string>();
            obj.Browser = It.IsAny<string>();
            obj.Action = It.IsAny<ActivityHistoryAction>();
            obj.OperatingSystem = It.IsAny<string>();

            // assert
            Assert.Equal(obj.Id, It.IsAny<Guid>());
            Assert.Equal(obj.ActivityDate, It.IsAny<DateTime>());
            Assert.Equal(obj.Device, It.IsAny<string>());
            Assert.Equal(obj.Browser, It.IsAny<string>());
            Assert.Equal(obj.Action, It.IsAny<ActivityHistoryAction>());
            Assert.Equal(obj.OperatingSystem, It.IsAny<string>());
        }
    }
}
