using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class UserFilterResponseDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int NumberOfPages { get; set; }
        public List<UserDto> Users { get; set; }
    }
}
