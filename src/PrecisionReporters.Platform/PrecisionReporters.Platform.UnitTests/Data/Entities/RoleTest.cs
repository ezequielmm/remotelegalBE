using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class RoleTest : BaseEntityTest<Role> 
    {
        [Fact]
        public void TestCopyFrom() {
            // arrange
            var itemMock = new Mock<Role>();

            itemMock.Object.CopyFrom(It.IsAny<Role>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Role>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Role>()), Times.Once);
        }
    }
}
