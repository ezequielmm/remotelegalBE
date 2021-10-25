using Moq;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class PostDepositionEditionDtoTest
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<PostDepositionEditionDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Video = "test.test";
            obj.GetCompositionId();

            obj.ConfigurationId = "notify-post-depo-complete";
            obj.IsComplete();

            // assert
            Assert.Equal("test", obj.GetCompositionId());
            Assert.True(obj.IsComplete());
        }
    }
}
