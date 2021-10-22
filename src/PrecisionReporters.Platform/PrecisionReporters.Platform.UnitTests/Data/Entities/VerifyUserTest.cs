using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class VerifyUserTest : BaseEntityTest<VerifyUser>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<VerifyUser>();

            itemMock.Object.CopyFrom(It.IsAny<VerifyUser>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<VerifyUser>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<VerifyUser>()), Times.Once);
        }
    }
}
