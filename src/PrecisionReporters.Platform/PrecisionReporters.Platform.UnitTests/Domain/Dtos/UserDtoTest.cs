using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class UserDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<UserDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.VerificationDate = It.IsAny<DateTime>();
            obj.ActivityHistory = It.IsAny<ActivityHistoryDto>();

            // assert
            Assert.Equal(obj.VerificationDate, It.IsAny<DateTime>());
            Assert.Equal(obj.ActivityHistory, It.IsAny<ActivityHistoryDto>());
        }
    }
}
