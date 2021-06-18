using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Collections.Generic;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class DepositionFactory
    {
        public static Deposition GetDeposition(Guid depositionId, Guid caseId)
        {
            var deposition = GetDepositionWithoutWitness(depositionId, caseId);
            deposition.Events = new List<DepositionEvent>();
            deposition.Participants = new List<Participant>{
                new Participant
                {
                    Id = Guid.NewGuid(),
                    Name = "witness1",
                    Email = "witness@email.com",
                    Role = ParticipantType.Witness
                }
            };
            deposition.IsOnTheRecord = true;
            return deposition;
        }


        public static Deposition GetDepositionWithoutWitness(Guid depositionId, Guid caseId)
        {
            return new Deposition
            {
                Id = depositionId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddHours(5),
                CreationDate = DateTime.UtcNow,
                Requester = new User
                {
                    Id = Guid.NewGuid(),
                    EmailAddress = "jbrown@email.com",
                    FirstName = "John",
                    LastName = "Brown",
                    IsAdmin = true
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
                    FileKey = "fileKey",
                    AddedBy = new User
                    {
                        Id = Guid.NewGuid(),
                        EmailAddress = "jbrown@email.com",
                        FirstName = "John",
                        LastName = "Brown",
                        IsAdmin = true
                    }
                },
                TimeZone = "America/New_York",
                Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness } }
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
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester=new User(){ EmailAddress = "testUser@mail.com" },
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
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "jbrown@email.com", FirstName = "John", LastName = "Brown"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "annewilson@email.com", FirstName = "Anne", LastName = "Wilson"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "juliarobinson@email.com", FirstName = "Julia", LastName = "Robinson"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
                    CreationDate = DateTime.UtcNow,
                    Requester = new User{ Id = Guid.NewGuid(), EmailAddress = "robertmatt@email.com", FirstName = "Robert", LastName = "Matt"}
                },
                new Deposition
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(5),
                    Participants = new List<Participant>{ new Participant { Role = ParticipantType.Witness } },
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

        public static List<DepositionEvent> GetDepositionEvents()
        {
            return new List<DepositionEvent>
            {
                new DepositionEvent
                {
                    EventType = EventType.OnTheRecord,
                    CreationDate = DateTime.UtcNow.AddSeconds(5)
                },
                new DepositionEvent
                {
                    EventType = EventType.OffTheRecord,
                    CreationDate = DateTime.UtcNow.AddMinutes(5)
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
                Participants = new List<Participant> { new Participant { Role = ParticipantType.Witness, Id = Guid.NewGuid(), Name = "witness1", Email = "witness@email.com" } },
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
                        UserId = isUser ? Guid.NewGuid() : (Guid?)null,
                        IsAdmitted = false,
                        Role = ParticipantType.Observer
                    }
                },
                Requester = new User
                {
                    EmailAddress = "requester@email.com"
                }
            };
        }

        public static DepositionDto GetDepositionDtoWithWitness(Guid depositionId, Guid caseId)
        {
            return new DepositionDto
            {
                Id = depositionId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddHours(5),
                CreationDate = DateTime.UtcNow,
                Requester = new UserDto
                {
                    Id = Guid.NewGuid(),
                    EmailAddress = "jbrown@email.com",
                    FirstName = "John",
                    LastName = "Brown",
                    IsAdmin = true
                },
                Room = new RoomDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"{caseId}_{Guid.NewGuid()}",
                    IsRecordingEnabled = true
                },
                Caption = DocumentFactory.GetDocumentDtoByDocumentType(DocumentType.Transcription),
                TimeZone = "ET",
                Participants = new List<ParticipantDto> 
                    { ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.CourtReporter) }
            };
        }

        public static EditDepositionDto GetEditDepositionDto()
        {
            return new EditDepositionDto
            {
                Deposition = GetDepositionDtoWithWitness(Guid.NewGuid(), Guid.NewGuid()),
                DeleteCaption = false
            };
        }
    }
}
