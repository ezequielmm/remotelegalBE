using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class ParticipantTest : BaseEntityTest<Participant>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Participant>();

            itemMock.Object.CopyFrom(It.IsAny<Participant>());
            itemMock.Object.DeviceInfoId = Guid.NewGuid();
            itemMock.Object.TwilioParticipant = new List<TwilioParticipant>() { It.IsAny<TwilioParticipant>()};
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Participant>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Participant>()), Times.Once);
        }
    }
}
