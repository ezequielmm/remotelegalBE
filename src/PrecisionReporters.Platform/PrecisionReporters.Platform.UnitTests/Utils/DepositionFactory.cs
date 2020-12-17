using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class DepositionFactory
    {
        public static Deposition GetDeposition(Guid depositionId, Guid caseId)
        {
            var depositon = GetDepositionWithoutWitness(depositionId, caseId);
            depositon.Witness = new Participant
            {
                Id = Guid.NewGuid(),
                Name = "witness1",
                Email = "witness@email.com"
            };
            depositon.Events = new List<DepositionEvent>();
            return depositon;
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
                Caption = new DepositionDocument
                {
                    Id = Guid.NewGuid(),
                    Name = "DepositionDocument_1",
                    FileKey = "fileKey"
                }
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

        public static List<DepositionDocument> GetDepositionDocumentList()
        {
            return new List<DepositionDocument>
            {
                new DepositionDocument
                {
                     Id = Guid.NewGuid(),
                     Name = "DepositionDocument_1",
                     FileKey = "DepositionDocument_1_FileKye"
                },
                new DepositionDocument
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
                Caption = new DepositionDocument
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

        public static Deposition GetDepositionWithParticipantEmail(string participantEmail)
        {
            return new Deposition
            {
                Participants = new List<Participant>
                {
                    new Participant
                    {
                        Email = participantEmail
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
