using PrecisionReporters.Platform.Shared.Helpers;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Helpers
{
    public class WorkingDayHelperTest
    {
        [Fact]
        public void Saturday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 18, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 22, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Sunday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 19, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 22, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 06, 19, 45, 00);
            DateTime expected = new DateTime(2021, 09, 08, 19, 45, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }


        [Fact]
        public void Tuesday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 7, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 9, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wednesday_offsett_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 8, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 10, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }


        [Fact]
        public void Thursday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 23);
            DateTime expected = new DateTime(2021, 09, 27);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Friday_offset_2()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 17, 08, 45, 00);
            DateTime expected = new DateTime(2021, 09, 21, 08, 45, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 48);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Saturday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 18, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 23, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Sunday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 19, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 23, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }



        [Fact]
        public void Monday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 06, 20, 45, 00);
            DateTime expected = new DateTime(2021, 09, 09, 20, 45, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tuesday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 7, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 10, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wednesday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 8, 16, 00, 00);
            DateTime expected = new DateTime(2021, 09, 13, 16, 00, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Thursday__offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 16, 08, 45, 00);
            DateTime expected = new DateTime(2021, 09, 21, 08, 45, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }


        [Fact]
        public void Friday_offset_3()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 17, 08, 45, 00);
            DateTime expected = new DateTime(2021, 09, 22, 08, 45, 00);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 72);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 06);
            DateTime expected = new DateTime(2021, 09, 10);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tuesday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 07);
            DateTime expected = new DateTime(2021, 09, 13);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wednesday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 08);
            DateTime expected = new DateTime(2021, 09, 14);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Thursday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 09);
            DateTime expected = new DateTime(2021, 09, 15);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Friday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 10);
            DateTime expected = new DateTime(2021, 09, 16);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Saturday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 11);
            DateTime expected = new DateTime(2021, 09, 17);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Sunday_offset_4()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 12);
            DateTime expected = new DateTime(2021, 09, 17);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 96);

            // Assert
            Assert.Equal(expected, actual);
        }


        [Fact]
        public void Monday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 06);
            DateTime expected = new DateTime(2021, 09, 13);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tuesday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 07);
            DateTime expected = new DateTime(2021, 09, 14);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wednesday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 08);
            DateTime expected = new DateTime(2021, 09, 15);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Thursday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 09);
            DateTime expected = new DateTime(2021, 09, 16);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Friday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 10);
            DateTime expected = new DateTime(2021, 09, 17);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Saturday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 11);
            DateTime expected = new DateTime(2021, 09, 20);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Sunday_offset_5()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 12);
            DateTime expected = new DateTime(2021, 09, 20);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 120);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 06);
            DateTime expected = new DateTime(2021, 09, 14);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tuesday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 07);
            DateTime expected = new DateTime(2021, 09, 15);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wednesday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 08);
            DateTime expected = new DateTime(2021, 09, 16);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Thursday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 09);
            DateTime expected = new DateTime(2021, 09, 17);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Friday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 10);
            DateTime expected = new DateTime(2021, 09, 20);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Saturday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 11);
            DateTime expected = new DateTime(2021, 09, 21);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Sunday_offset_6()
        {
            // Arrange
            DateTime date = new DateTime(2021, 09, 12);
            DateTime expected = new DateTime(2021, 09, 21);
            DateTime actual;

            // Act
            actual = WorkingDayHelper.WorkingDayUsingOffset(date, 144);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
