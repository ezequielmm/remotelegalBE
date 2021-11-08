using PrecisionReporters.Platform.Shared.Extensions;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Shared.Extensions
{
    public class StringExtensionsTest
    {
        [Fact]
        public void ToHypenCase()
        {
            //Arrange
            var expectedResult = "text-to-test-number-one";
            var textForTesting = "Text To Test Number One";

            //Act
            var result = textForTesting.ToHypenCase();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }
    }
}
