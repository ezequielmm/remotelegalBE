using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class UserFactory
    {
        public static User GetUserByGivenId(Guid id)
        {
            return new User
            {
                Id = id,
                CreationDate = DateTime.UtcNow,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = "User1@TestMail.com",
                Password = "123456",
                PhoneNumber = "1234567890"
            };
        }

        public static User GetUserByGivenEmail(string email)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsAdmin = false
            };
        }

        public static User GetUserByGivenIdAndEmail(Guid id, string email)
        {
            return new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890"
            };
        }

        public static User GetGuestUserByGivenIdAndEmail(Guid id, string email)
        {
            return new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = true
            };
        }
    }
}
