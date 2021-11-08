using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.Shared.Extensions;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Shared.Extensions
{
    public class ResultExtensionsTest
    {
        [Fact]
        public void GetErrorMessage_WithType_Ok()
        {
            //Arrange
            var testResult = Result.Ok().ToResult<MemberDto>();

            //Act
            var result = testResult.GetErrorMessage();

            //Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetErrorMessage_WithType_Fail()
        {
            //Arrange
            var errorText = "Invalid Input";
            var testResult = Result.Fail(new InvalidInputError(errorText)).ToResult<MemberDto>();
            //Act
            var result = testResult.GetErrorMessage();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(errorText, result);
        }

        [Fact]
        public void GetErrorMessage_Ok()
        {
            //Arrange
            var testResult = Result.Ok();

            //Act
            var result = testResult.GetErrorMessage();

            //Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetErrorMessage_Fail()
        {
            //Arrange
            var errorText = "Invalid Input";
            var testResult = Result.Fail(new InvalidInputError(errorText));
            //Act
            var result = testResult.GetErrorMessage();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(errorText, result);
        }
    }
}
