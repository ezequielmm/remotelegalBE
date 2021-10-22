using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DocumentTest : BaseEntityTest<Document>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Document>();

            itemMock.Object.CopyFrom(It.IsAny<Document>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Document>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Document>()), Times.Once);
        }
    }
}
