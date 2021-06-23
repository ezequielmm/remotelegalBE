using PrecisionReporters.Platform.Data.Entities;
using System;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Collections.Generic;

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

        public static UserDto GetCreateUserDto()
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                EmailAddress = "mock@mail.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "2105428027",
                CompanyAddress = "Mock Address",
                CompanyName = "Mock & Co",
                IsAdmin = false,
                IsGuest = false
            };
        }

        public static List<User> GetUserList()
        {
            return new List<User> {
                new User
                {
                    FirstName = "John",
                    LastName = "Doe",
                    CompanyAddress = "Fake street 1234",
                    CompanyName = "Fake company name LLC",
                    EmailAddress = "testUser@mail.com",
                    PhoneNumber = "2233222333",
                    IsAdmin = false,
                    IsGuest = false,
                    Password = "1234abcD",
                    SId = "testId"
                },
                new User
                {
                    FirstName = "Mock",
                    LastName = "Mockium",
                    CompanyAddress = "Fake street 5678",
                    CompanyName = "Mock company name LLC",
                    EmailAddress = "testUser2@mail.com",
                    PhoneNumber = "2233222334",
                    IsAdmin = false,
                    IsGuest = false,
                    Password = "1234abcD",
                    SId = "testId2"
                }
            };
        }
    }
}
