using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class TranscriptionsHubDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<TranscriptionsHubDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.SampleRate = It.IsAny<int>();

            // assert
            Assert.Equal(obj.SampleRate, It.IsAny<int>());
        }
    }
}
