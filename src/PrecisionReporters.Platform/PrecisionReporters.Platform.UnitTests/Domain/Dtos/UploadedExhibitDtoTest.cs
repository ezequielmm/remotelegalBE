using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class UploadedExhibitDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<UploadedExhibitDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.ResourceId = It.IsAny<string>();
            obj.DocumentId = It.IsAny<Guid>();

            // assert
            Assert.Equal(obj.ResourceId, It.IsAny<string>());
            Assert.Equal(obj.DocumentId, It.IsAny<Guid>());
        }
    }
}
