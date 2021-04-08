using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class BringAllToMeDto
    {
        public string DocumentLocation { get; set; }
        public Guid? UserId { get; set; }
    }
}
