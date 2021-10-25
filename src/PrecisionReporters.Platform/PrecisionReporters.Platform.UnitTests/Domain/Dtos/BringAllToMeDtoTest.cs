using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class BringAllToMeDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<BringAllToMeDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.DocumentLocation = It.IsAny<string>();
            obj.UserId = It.IsAny<Guid>();

            // assert
            Assert.Equal(obj.DocumentLocation, It.IsAny<string>());
            Assert.Equal(obj.UserId, It.IsAny<Guid>());
        }
    }
}
