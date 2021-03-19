using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

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
