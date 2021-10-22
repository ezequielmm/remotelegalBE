using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class BreakRoomTest : BaseEntityTest<BreakRoom>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<BreakRoom>();

            itemMock.Object.CopyFrom(It.IsAny<BreakRoom>());
            itemMock.Object.RoomId = Guid.NewGuid();
            itemMock.Setup(x => x.CopyFrom(It.IsAny<BreakRoom>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<BreakRoom>()), Times.Once);
        }
    }
}
