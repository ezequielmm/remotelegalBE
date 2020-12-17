using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class DocumentDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public long Size { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public UserOutputDto AddedBy { get; set; }
    }
}