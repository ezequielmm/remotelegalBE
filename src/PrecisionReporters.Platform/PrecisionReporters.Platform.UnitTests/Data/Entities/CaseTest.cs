using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class CaseTest : BaseEntityTest<Case>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Case>();

            itemMock.Object.CopyFrom(It.IsAny<Case>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Case>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Case>()), Times.Once);
        }
    }
}
