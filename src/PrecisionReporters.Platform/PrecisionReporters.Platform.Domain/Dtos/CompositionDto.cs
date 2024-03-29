﻿using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CompositionDto
    {
        public Guid Id { get; set; }
        public DateTime CreationDate { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Guid RoomId { get; set; }
        public string SId { get; set; }
        public string Url { get; set; }
        public string MediaUrl { get; set; }
    }
}
