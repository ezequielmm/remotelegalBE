using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class WebSocketDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<WebSocketDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.OffRecord = It.IsAny<bool>();

            // assert
            Assert.Equal(obj.OffRecord, It.IsAny<bool>());
        }
    }
}
