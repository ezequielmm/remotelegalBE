using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class ActivityHistoryTest : BaseEntityTest<ActivityHistory>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<ActivityHistory>();

            itemMock.Object.CopyFrom(It.IsAny<ActivityHistory>());
            itemMock.Object.ActionDetails = "test details";
            itemMock.Setup(x => x.CopyFrom(It.IsAny<ActivityHistory>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<ActivityHistory>()), Times.Once);
        }
    }
}
