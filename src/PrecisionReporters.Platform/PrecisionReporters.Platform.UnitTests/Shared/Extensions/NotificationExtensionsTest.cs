using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Extensions;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Shared.Extensions
{
    public class NotificationExtensionsTest
    {
        [Fact]
        public void GetDepositionSignalRGroupName()
        {
            //Arrange
            var testGuid = Guid.NewGuid();
            var expectedResult = $"{ApplicationConstants.DepositionGroupName}{testGuid}";

            //Act
            var result = testGuid.GetDepositionSignalRGroupName();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetDepositionSignalRAdminsGroupName()
        {
            //Arrange
            var testGuid = Guid.NewGuid();
            var expectedResult = $"{ApplicationConstants.DepositionAdminsGroupName}{testGuid}";

            //Act
            var result = testGuid.GetDepositionSignalRAdminsGroupName();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }
    }
}
