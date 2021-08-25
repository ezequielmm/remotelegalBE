using System;

namespace PrecisionReporters.Platform.Shared.Dtos
{
    public class DocumentDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string FilePath { get; set; }
        public long Size { get; set; }
        public Guid AddedBy { get; set; }
        public string DocumentType { get; set; }
        public string Type { get; set; }
        public Guid DepositionId { get; set; }
    }
}