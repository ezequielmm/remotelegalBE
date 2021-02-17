using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class CaseFactory
    {
        public static List<Case> GetCases()
        {
            return new List<Case>
            {
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = new User{ Id = Guid.NewGuid(), EmailAddress = "jbrown@email.com", FirstName = "John", LastName = "Brown"}
                },
                 new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase2",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = new User{ Id = Guid.NewGuid(), EmailAddress = "annewilson@email.com", FirstName = "Anne", LastName = "Wilson"}
                },
                  new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase3",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = new User{ Id = Guid.NewGuid(), EmailAddress = "juliarobinson@email.com", FirstName = "Julia", LastName = "Robinson"}
                },
                  new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase4",
                    CreationDate = DateTime.UtcNow,
                    AddedBy =new User{ Id = Guid.NewGuid(), EmailAddress = "helenlauphan@email.com", FirstName = "Helen", LastName = "Lauphan"}
                },
                  new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase5",
                    CreationDate = DateTime.UtcNow,
                    AddedBy = new User{ Id = Guid.NewGuid(), EmailAddress = "robertmatt@email.com", FirstName = "Robert", LastName = "Matt"}
                }
            };
        }
    }
}
