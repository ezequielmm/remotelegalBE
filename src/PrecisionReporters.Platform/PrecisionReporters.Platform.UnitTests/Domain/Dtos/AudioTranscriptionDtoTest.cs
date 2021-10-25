using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class AudioTranscriptionDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<AudioTranscriptionDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.User = It.IsAny<string>();
            obj.AudioText = It.IsAny<string>();
            obj.StartDate = It.IsAny<DateTimeOffset>();
            obj.IsFinished = It.IsAny<bool>();

            // assert
            Assert.Equal(obj.User, It.IsAny<string>());
            Assert.Equal(obj.AudioText, It.IsAny<string>());
            Assert.Equal(obj.StartDate, It.IsAny<DateTimeOffset>());
            Assert.Equal(obj.IsFinished, It.IsAny<bool>());
        }
    }
}
