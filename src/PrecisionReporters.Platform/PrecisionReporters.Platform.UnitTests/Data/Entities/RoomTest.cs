using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class RoomTest : BaseEntityTest<Room>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Room>();

            itemMock.Object.CopyFrom(It.IsAny<Room>());
            itemMock.Object.RecordingEndDate = It.IsAny<DateTime>();
            itemMock.Object.RecordingDuration = It.IsAny<int>();
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Room>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Room>()), Times.Once);
        }
    }
}
