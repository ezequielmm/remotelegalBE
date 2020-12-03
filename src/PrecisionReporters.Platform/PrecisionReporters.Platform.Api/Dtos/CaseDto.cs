﻿using System;
using System.Collections.Generic;
using PrecisionReporters.Platform.Api.Controllers;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CaseDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Name { get; set; }
        public string CaseNumber { get; set; }
        public Guid AddedById { get; set; }
    }
}
