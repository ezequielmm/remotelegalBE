using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class VerifyUserFactory
    {
        public static VerifyUser GetVerifyUser(User user)
        {
            return new VerifyUser
            {
                CreationDate = DateTime.Now,
                Id = Guid.NewGuid(),
                IsUsed = false,
                User = user
            };
        }

        public static VerifyUser GetVerifyUserByGivenId(Guid id, DateTime date, User user)
        {
            return new VerifyUser
            {
                CreationDate = date,
                Id = id,
                IsUsed = false,
                User = user
            };
        }

        public static VerifyUser GetUsedVerifyUserByGivenId(Guid id, DateTime date, User user)
        {
            return new VerifyUser
            {
                CreationDate = date,
                Id = id,
                IsUsed = true,
                User = user
            };
        }
    }
}
