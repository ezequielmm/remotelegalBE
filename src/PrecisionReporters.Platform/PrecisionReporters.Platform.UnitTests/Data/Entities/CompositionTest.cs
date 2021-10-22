using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class CompositionTest : BaseEntityTest<Composition>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Composition>();

            itemMock.Object.CopyFrom(It.IsAny<Composition>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Composition>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Composition>()), Times.Once);
        }
    }
}
