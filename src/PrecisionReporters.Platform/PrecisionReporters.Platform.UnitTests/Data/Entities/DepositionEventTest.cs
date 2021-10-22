using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DepositionEventTest : BaseEntityTest<DepositionEvent>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<DepositionEvent>();

            itemMock.Object.CopyFrom(It.IsAny<DepositionEvent>());
            itemMock.Object.Deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            itemMock.Object.DepositionId = Guid.NewGuid();
            itemMock.Object.User = UserFactory.GetUserByGivenEmail("test@mock.com");
            itemMock.Setup(x => x.CopyFrom(It.IsAny<DepositionEvent>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<DepositionEvent>()), Times.Once);
        }
    }
}
