using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class JoinToRealRoomDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<JoinToRealRoomDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.RoomName = It.IsAny<string>();
            obj.Token = It.IsAny<string>();

            // assert
            Assert.Equal(obj.RoomName, It.IsAny<string>());
            Assert.Equal(obj.Token, It.IsAny<string>());
        }
    }
}
