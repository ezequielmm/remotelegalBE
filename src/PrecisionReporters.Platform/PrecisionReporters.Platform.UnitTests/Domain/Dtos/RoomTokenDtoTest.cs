using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class RoomTokenDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<RoomTokenDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Token = It.IsAny<string>();

            // assert
            Assert.Equal(obj.Token, It.IsAny<string>());
        }
    }
}
