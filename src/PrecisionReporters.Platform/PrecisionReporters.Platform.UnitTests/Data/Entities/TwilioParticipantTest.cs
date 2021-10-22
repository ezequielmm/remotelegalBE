using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class TwilioParticipantTest : BaseEntityTest<TwilioParticipant>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<TwilioParticipant>();

            itemMock.Object.CopyFrom(It.IsAny<TwilioParticipant>());
            itemMock.Object.ParticipantId = Guid.NewGuid();
            itemMock.Setup(x => x.CopyFrom(It.IsAny<TwilioParticipant>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<TwilioParticipant>()), Times.Once);
        }
    }
}
