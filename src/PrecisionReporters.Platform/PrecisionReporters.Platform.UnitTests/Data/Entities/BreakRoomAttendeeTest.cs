using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class BreakRoomAttendeeTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<BreakRoomAttendee>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object; 
            obj.BreakRoomId = It.IsAny<Guid>(); 
            
            // assert
            Assert.Equal(obj.BreakRoomId, It.IsAny<Guid>());
        }
    }
}
