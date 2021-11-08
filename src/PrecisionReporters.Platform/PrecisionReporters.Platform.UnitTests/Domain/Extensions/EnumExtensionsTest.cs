using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Extensions
{
    public class EnumExtensionsTest
    {
        public EnumExtensionsTest()
        {

        }

        [Fact]
        public void GetDescription()
        {
            //Arrange
            var enumTest = USTimeZone.AT;
            var expectedResult = "America/Puerto_Rico";
            var expectedTypeResult = typeof(string);

            //Act
            var result = enumTest.GetDescription();

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.NotNull(result);
            Assert.IsType(expectedTypeResult, result);
        }

        [Fact]
        public void ParseDescriptionToEnum()
        {
            //Arrange
            var expectedResult = USTimeZone.AT;
            var stringDescriptionTest = "America/Puerto_Rico";
            var expectedTypeResult = typeof(USTimeZone);

            //Act
            var result = stringDescriptionTest.ParseDescriptionToEnum<USTimeZone>();

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.IsType(expectedTypeResult, result);
        }

    }
}
