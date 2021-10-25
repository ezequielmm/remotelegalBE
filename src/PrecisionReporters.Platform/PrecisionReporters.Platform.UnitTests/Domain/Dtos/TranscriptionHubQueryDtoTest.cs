using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class TranscriptionHubQueryDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<TranscriptionHubQueryDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.DepositionId = It.IsAny<string>();
            obj.SampleRate = It.IsAny<int>();

            // assert
            Assert.Equal(obj.DepositionId, It.IsAny<string>());
            Assert.Equal(obj.SampleRate, It.IsAny<int>());
        }
    }
}
