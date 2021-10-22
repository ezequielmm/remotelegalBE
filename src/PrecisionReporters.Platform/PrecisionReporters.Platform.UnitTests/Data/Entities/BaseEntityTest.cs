using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public abstract class BaseEntityTest<T> where T : BaseEntity<T>
    {
        [Fact]
        public void BaseFunctionsTest() {
            // arrange
            var itemMock = new Mock<T>();

            itemMock.Object.CopyFrom(It.IsAny<T>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<T>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<T>()), Times.Once);

        }
    }
}
