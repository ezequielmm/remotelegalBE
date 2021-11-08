using PrecisionReporters.Platform.Domain.Extensions;
using System;
using System.Linq;
using TimeZoneConverter;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Extensions
{
    public class DateTimeExtensionsTest
    {
        private readonly DateTime TestDate;
        private readonly string TimeZone;
        private readonly TimeZoneInfo TimeZoneInfo;
        private readonly string TimeZoneAbbreviation;

        public DateTimeExtensionsTest()
        {
            TestDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            TimeZone = "America/New_York";
            TimeZoneInfo = TZConvert.GetTimeZoneInfo(TimeZone);
            TimeZoneAbbreviation = Enum.GetValues(typeof(Platform.Domain.Enums.USTimeZone))
                .Cast<Platform.Domain.Enums.USTimeZone>()
                .FirstOrDefault(x => x.GetDescription() == TimeZone).ToString();
        }

        [Fact]
        public void ConvertTimeZone()
        {
            //Arrange
            var expectedType = typeof(string);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(TestDate, TimeZoneInfo);
            var expectedResult = $"{convertedTime:hh:mm tt} {TimeZoneAbbreviation}";

            //Act
            var result = TestDate.ConvertTimeZone(TimeZone);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void ConvertTime()
        {
            //Arrange
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(TestDate, TimeZoneInfo);
            var expectedresult = convertedTime.ToShortTimeString();
            var expectedType = typeof(string);

            //Act
            var result = TestDate.ConvertTime("America/New_York");

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedresult, result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetConvertedTime()
        {
            //Arrange
            var expectedResult = TimeZoneInfo.ConvertTimeFromUtc(TestDate, TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Id));
            var expectedType = typeof(DateTime);

            //Act
            var result = TestDate.GetConvertedTime(TimeZone);

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetFormattedDateTime()
        {
            //Arrange
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(TestDate, TimeZoneInfo);
            var expectedResult = $"{convertedTime:MMM d, yyyy hh:mm tt} {TimeZoneAbbreviation}";
            var expectedType = typeof(string);

            //Act
            var result = TestDate.GetFormattedDateTime(TimeZone);

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetWithSpecificTimeZone()
        {
            //Arrange
            var expectedResult = TimeZoneInfo.ConvertTime(TestDate, TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Id));
            var expectedType = typeof(DateTime);

            //Act
            var result = TestDate.GetWithSpecificTimeZone(TimeZone);

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetDateTimeToSeconds()
        {
            //Arrange
            var expectedResult = new DateTimeOffset(TestDate.Year, TestDate.Month, TestDate.Day, TestDate.Hour, TestDate.Minute, TestDate.Second, TimeSpan.Zero).ToUnixTimeSeconds();
            var expectedType = typeof(long);

            //Act
            var result = TestDate.GetDateTimeToSeconds();

            //Assert
            Assert.Equal(expectedResult, result);
            Assert.True(result > 0);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetTimeZoneInfo_Should_Ok()
        {
            //Arrange
            var expectedType = typeof(TimeZoneInfo);

            //Act
            var result = TimeZone.GetTimeZoneInfo();

            //Assert
            Assert.NotNull(result);
            Assert.IsType(expectedType, result);
        }

        [Fact]
        public void GetTimeZoneInfo_Should_Fail_Return_Null()
        {
            //Arrange
            var timeZone = "ET";

            //Act
            var result = timeZone.GetTimeZoneInfo();

            //Assert
            Assert.Null(result);
        }
    }
}
