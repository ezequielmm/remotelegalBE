using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class AnnotationEventTest : BaseEntityTest<AnnotationEvent>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<AnnotationEvent>();

            itemMock.Object.CopyFrom(It.IsAny<AnnotationEvent>());
            itemMock.Object.AuthorId = Guid.NewGuid();
            itemMock.Object.Document = DocumentFactory.GetDocument();
            itemMock.Setup(x => x.CopyFrom(It.IsAny<AnnotationEvent>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<AnnotationEvent>()), Times.Once);
        }
    }
}
