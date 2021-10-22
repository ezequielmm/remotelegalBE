using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System.Collections.Generic;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class UserTest : BaseEntityTest<User>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<User>();

            itemMock.Object.CopyFrom(It.IsAny<User>());
            itemMock.Object.DocumentUserDepositions = new List<DocumentUserDeposition>() { It.IsAny<DocumentUserDeposition>() };
            itemMock.Object.MemberOn = It.IsAny<ICollection<Member>>();
            itemMock.Setup(x => x.CopyFrom(It.IsAny<User>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<User>()), Times.Once);
        }
    }
}
