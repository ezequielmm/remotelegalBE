using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DepositionTest : BaseEntityTest<Deposition>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Deposition>();

            itemMock.Object.CopyFrom(It.IsAny<Deposition>());
            itemMock.Object.PreRoomId = Guid.NewGuid();
            itemMock.Object.DocumentUserDepositions = new List<DocumentUserDeposition>() { It.IsAny<DocumentUserDeposition>() };
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Deposition>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Deposition>()), Times.Once);
        }
    }
}
