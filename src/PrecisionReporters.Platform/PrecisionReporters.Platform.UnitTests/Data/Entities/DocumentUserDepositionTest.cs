using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DocumentUserDepositionTest : BaseEntityTest<DocumentUserDeposition>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<DocumentUserDeposition>();

            itemMock.Object.CopyFrom(It.IsAny<DocumentUserDeposition>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<DocumentUserDeposition>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<DocumentUserDeposition>()), Times.Once);
        }
    }
}
