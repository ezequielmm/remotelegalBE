using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class MediaStampDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<MediaStampDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.StampLabel = It.IsAny<string>();
            obj.DepositionId = It.IsAny<Guid>();
            obj.CreationDate = It.IsAny<DateTimeOffset>();

            // assert
            Assert.Equal(obj.StampLabel, It.IsAny<string>());
            Assert.Equal(obj.DepositionId, It.IsAny<Guid>());
            Assert.Equal(obj.CreationDate, It.IsAny<DateTimeOffset>());
        }
    }
}
