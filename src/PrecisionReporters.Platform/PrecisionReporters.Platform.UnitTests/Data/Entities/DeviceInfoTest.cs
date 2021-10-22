using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DeviceInfoTest : BaseEntityTest<DeviceInfo>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<DeviceInfo>();

            itemMock.Object.CopyFrom(It.IsAny<DeviceInfo>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<DeviceInfo>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<DeviceInfo>()), Times.Once);
        }
    }
}
