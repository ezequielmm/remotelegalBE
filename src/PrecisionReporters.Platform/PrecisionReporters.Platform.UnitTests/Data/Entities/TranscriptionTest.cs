using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class TranscriptionTest : BaseEntityTest<Transcription>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Transcription>();

            itemMock.Object.CopyFrom(It.IsAny<Transcription>());
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Transcription>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Transcription>()), Times.Once);
        }
    }
}
