﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Participant : BaseEntity<Participant>
    {
        [Required]
        public ParticipantRole Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        [ForeignKey(nameof(User))]
        public Guid? UserId { get; set; }
        public User User { get; set; }

        public override void CopyFrom(Participant entity)
        {
            Role = entity.Role;
            Name = entity.Name;
            Email = entity.Email;
            Phone = entity.Phone;
            User = entity.User;
        }
    }
}
