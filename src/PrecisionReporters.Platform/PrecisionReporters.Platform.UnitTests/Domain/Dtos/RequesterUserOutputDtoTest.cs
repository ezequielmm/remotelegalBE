using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class RequesterUserOutputDtoTest
    {
        [Fact]
        public void TestProperties()
        {
            // arrange
            var itemMock = new Mock<RequesterUserOutputDto>();

            itemMock.SetupAllProperties();
            var obj = itemMock.Object;
            obj.Id = It.IsAny<Guid>();
            obj.FirstName = It.IsAny<string>();
            obj.LastName = It.IsAny<string>();
            obj.EmailAddress = It.IsAny<string>();
            obj.PhoneNumber = It.IsAny<string>();
            obj.CompanyName = It.IsAny<string>();

            // assert
            Assert.Equal(obj.Id, It.IsAny<Guid>());
            Assert.Equal(obj.FirstName, It.IsAny<string>());
            Assert.Equal(obj.LastName, It.IsAny<string>());
            Assert.Equal(obj.EmailAddress, It.IsAny<string>());
            Assert.Equal(obj.PhoneNumber, It.IsAny<string>());
            Assert.Equal(obj.CompanyName, It.IsAny<string>());
        }
    }
}
