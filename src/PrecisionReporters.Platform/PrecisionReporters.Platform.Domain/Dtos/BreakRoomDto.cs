using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class BreakRoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsLocked { get; set; }
        public List<UserOutputDto> CurrentAttendes { get; set; }
    }
}
