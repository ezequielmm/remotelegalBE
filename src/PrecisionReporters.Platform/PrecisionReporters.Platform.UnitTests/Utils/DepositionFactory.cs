using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class DepositionFactory
    {
        public static Deposition GetDeposition(Guid depositionId, Guid caseId)
        {
            var deposition = GetDepositionWithoutWitness(depositionId, caseId);
            deposition.Witness = new Participant
            {
                Id = Guid.NewGuid(),
                Name = "witness1",
                Email = "witness@email.com"
            };
            deposition.Events = new List<DepositionEvent>();
            deposition.Participants = new List<Participant>();
            deposition.IsOnTheRecord = true;
            return deposition;
        }


        public static Deposition GetDepositionWithoutWitness(Guid depositionId, Guid caseId)
        {
            return new Deposition
            {
                Id = depositionId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                WitnessId = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Requester = new User
                {
                    Id = Guid.NewGuid(),
                    EmailAddress = "jbrown@email.com",
                    FirstName = "John",
                    LastName = "Brown"
                },
                Room = new Room
                {
                    Id = Guid.NewGuid(),
                    Name = $"{caseId}_{Guid.NewGuid()}",
                    IsRecordingEnabled = true
                },
                Caption = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = "DepositionDocument_1",
                    FileKey = "fileKey"
                },
                TimeZone = "EST"
            };
        }

        public static List<Deposition> GetDepositionList()
        {
            return new List<Deposition> {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow
                }
            };
        }

        public static List<Deposition> GetDepositionsWithRequesters()
        {
            return new List<Deposition> {
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "jbrown@email.com", FirstName = "John", LastName = "Brown"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "annewilson@email.com", FirstName = "Anne", LastName = "Wilson"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "juliarobinson@email.com", FirstName = "Julia", LastName = "Robinson"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "robertmatt@email.com", FirstName = "Robert", LastName = "Matt"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    WitnessId = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "helenlauphan@email.com", FirstName = "Helen", LastName = "Lauphan"}
                }
            };
        }

        public static List<Document> GetDocumentList()
        {
            return new List<Document>
            {
                new Document
                {
                     Id = Guid.NewGuid(),
                     Name = "DepositionDocument_1",
                     FileKey = "DepositionDocument_1_FileKye"
                },
                new Document
                {
                    Id = Guid.NewGuid(),
                    Name = "DepositionDocument_2",
                    FileKey = "DepositionDocument_2_FileKye"
                }
            };
        }

        public static Deposition GetDepositionWithoutRequester(Guid depositionId, Guid caseId)
        {
            return new Deposition
            {
                Id = depositionId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                WitnessId = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Room = new Room
                {
                    Id = Guid.NewGuid(),
                    Name = $"{caseId}_{Guid.NewGuid()}",
                    IsRecordingEnabled = true
                },
                Caption = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = "DepositionDocument_1",
                    FileKey = "fileKey"
                },
                Witness = new Participant
                {
                    Id = Guid.NewGuid(),
                    Name = "witness1",
                    Email = "witness@email.com"
                }
            };
        }

        public static Deposition GetDepositionWithParticipantEmail(string participantEmail, bool isUser = true)
        {
            return new Deposition
            {
                Participants = new List<Participant>
                {
                    new Participant
                    {
                        Email = participantEmail,
                        UserId = isUser ? Guid.NewGuid() : (Guid?)null
                    }
                },
                Requester = new User
                {
                    EmailAddress = "requester@email.com"
                }
            };
        }
    }
}
