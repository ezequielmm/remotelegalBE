﻿using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }     
        public string PhoneNumber { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsGuest { get; set; }
        public DateTime VerificationDate { get; set; }
        public ActivityHistoryDto? ActivityHistory { get; set; }
    }
}
