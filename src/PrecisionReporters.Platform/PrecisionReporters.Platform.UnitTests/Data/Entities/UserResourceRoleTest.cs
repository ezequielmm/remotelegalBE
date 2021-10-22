using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class UserResourceRoleTest : BaseEntityTest<UserResourceRole>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<UserResourceRole>();

            itemMock.Object.CopyFrom(It.IsAny<UserResourceRole>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<UserResourceRole>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<UserResourceRole>()), Times.Once);
        }
    }
}
