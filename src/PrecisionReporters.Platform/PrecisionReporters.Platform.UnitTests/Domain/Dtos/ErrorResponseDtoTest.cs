using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class ErrorResponseDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<ErrorResponseDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Message = It.IsAny<string>();
            obj.Error = It.IsAny<Exception>();

            // assert
            Assert.Equal(obj.Message, It.IsAny<string>());
            Assert.Equal(obj.Error, It.IsAny<Exception>());
        }
    }
}
