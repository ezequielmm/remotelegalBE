using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class RoomCallbackDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<RoomCallbackDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.RecordingSid = It.IsAny<string>();
            obj.Url = It.IsAny<string>();
            obj.RoomName = It.IsAny<string>();
            obj.ParticipantIdentity = It.IsAny<string>();

            // assert
            Assert.Equal(obj.RecordingSid, It.IsAny<string>());
            Assert.Equal(obj.Url, It.IsAny<string>());
            Assert.Equal(obj.RoomName, It.IsAny<string>());
            Assert.Equal(obj.ParticipantIdentity, It.IsAny<string>());
        }
    }
}
