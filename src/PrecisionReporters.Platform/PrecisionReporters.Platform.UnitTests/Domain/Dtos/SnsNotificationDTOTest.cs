using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class SnsNotificationDTOTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<SnsNotificationDTO>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Message = It.IsAny<string>();
            obj.Data = It.IsAny<Object>();

            // assert
            Assert.Equal(obj.Message, It.IsAny<string>());
            Assert.Equal(obj.Data, It.IsAny<Object>());
        }
    }
}
