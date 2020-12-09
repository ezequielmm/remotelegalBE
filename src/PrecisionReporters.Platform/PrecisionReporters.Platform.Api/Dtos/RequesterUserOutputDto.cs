﻿using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class RequesterUserOutputDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string CompanyName { get; set; }
    }
}