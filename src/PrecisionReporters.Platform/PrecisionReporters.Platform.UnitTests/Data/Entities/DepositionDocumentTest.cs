using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DepositionDocumentTest : BaseEntityTest<DepositionDocument>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<DepositionDocument>();

            itemMock.Object.CopyFrom(It.IsAny<DepositionDocument>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<DepositionDocument>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<DepositionDocument>()), Times.Once);
        }
    }
}
