using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class SystemSettingsTest : BaseEntityTest<SystemSettings>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<SystemSettings>();

            itemMock.Object.CopyFrom(It.IsAny<SystemSettings>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<SystemSettings>())).Verifiable();
            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<SystemSettings>()), Times.Once);
        }
    }
}
