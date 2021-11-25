using Moq;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Entities
{
    public class ParticipantTest : BaseEntityTest<Participant>
    {
        [Fact]
        public void TestCopyFrom()
        {
            // arrange
            var itemMock = new Mock<Participant>();

            itemMock.Object.CopyFrom(It.IsAny<Participant>());
            itemMock.Object.DeviceInfoId = Guid.NewGuid();
            itemMock.Object.TwilioParticipant = new List<TwilioParticipant>() { It.IsAny<TwilioParticipant>()};
            itemMock.Setup(x => x.CopyFrom(It.IsAny<Participant>())).Verifiable();

            // assert
            itemMock.Verify(x => x.CopyFrom(It.IsAny<Participant>()), Times.Once);
        }

        [Fact]
        public void GetFullName_ReturnOnlyName_WhenLastnameIsNull()
        {
            // Arrange
            var name = "ParticipantName";
            string lastName = null;
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            participant.Name = name;
            participant.LastName = lastName;

            // Act
            var result = participant.GetFullName();

            // Assert
            Assert.Equal(name, result);
        }

        [Fact]
        public void GetFullName_ReturnOnlyName_WhenLastnameIsWhiteSpace()
        {
            // Arrange
            var name = "ParticipantName";
            string lastName = " ";
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            participant.Name = name;
            participant.LastName = lastName;

            // Act
            var result = participant.GetFullName();

            // Assert
            Assert.Equal(name, result);
        }

        [Fact]
        public void GetFullName_ReturnFullName_WhenLastNameIsNotNull()
        {
            // Arrange
            var name = "MockName";
            string lastName = "MockLastName";
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            participant.Name = name;
            participant.LastName = lastName;

            // Act
            var result = participant.GetFullName();

            // Assert
            Assert.Equal($"{name} {lastName}", result);
        }
    }
}
