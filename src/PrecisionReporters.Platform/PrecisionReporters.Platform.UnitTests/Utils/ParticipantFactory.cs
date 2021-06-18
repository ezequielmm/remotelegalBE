using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class ParticipantFactory
    {
        public static Participant GetParticipant(Guid depositionId)
        {
            return new Participant
            {
                Id = Guid.NewGuid(),
                Email = "participant@email.com",
                Name = "Participant Email",
                IsMuted = false,
                DepositionId = depositionId
            };
        }

        public static ParticipantDto GetParticipantDtoByGivenRole(ParticipantType role)
        {
            return new ParticipantDto
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTimeOffset.Now,
                Email = $"{role}@mockEmail.Com",
                Name = "Name",
                Phone = "2105428027",
                Role = role.ToString(),
                User = new UserOutputDto { Id = Guid.NewGuid() }
            };
        }

        public static ParticipantDto GetParticipantDto()
        {
            return new ParticipantDto
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTimeOffset.Now,
                Email = "participant@mockEmail.Com",
                Name = "Name",
                Phone = "2105428027",
                Role = ParticipantType.CourtReporter.ToString(),
                User = new UserOutputDto { Id = Guid.NewGuid() }
            };
        }

        public static CreateParticipantDto GetCreateParticipantDtoByGivenRole(ParticipantType role)
        {
            return new CreateParticipantDto
            {
                Email = $"{role}@mockEmail.Com",
                Name = "Name",
                Phone = "2105428027",
                Role = role,
                IsMuted = false
            };
        }

        public static ParticipantStatusDto GetParticipantSatus()
        {
            return new ParticipantStatusDto
            {
                Email = "participant@email.com",
                IsMuted = true
            };
        }

        public static User GetAdminUser()
        {
            return new User
            {
                Id = Guid.NewGuid(),
                IsAdmin = true,
            };
        }

        public static User GetNotAdminUser()
        {
            return new User
            {
                Id = Guid.NewGuid(),
                IsAdmin = false,
            };
        }
    }
}
