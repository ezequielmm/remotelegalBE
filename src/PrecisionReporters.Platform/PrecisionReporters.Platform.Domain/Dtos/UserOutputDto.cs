using System;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class UserOutputDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string ParticipantAlias { get; set; }
        public bool IsGuest { get; set; }

        public UserOutputDto() { }

        public UserOutputDto(User user)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            EmailAddress = user.EmailAddress;
            IsGuest = user.IsGuest;
        }
    }
}