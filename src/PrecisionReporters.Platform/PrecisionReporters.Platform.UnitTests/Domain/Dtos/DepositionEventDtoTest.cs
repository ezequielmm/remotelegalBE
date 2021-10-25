using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class DepositionEventDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<DepositionEventDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.User = It.IsAny<UserOutputDto>();

            // assert
            Assert.Equal(obj.User, It.IsAny<UserOutputDto>());
        }
    }
}
