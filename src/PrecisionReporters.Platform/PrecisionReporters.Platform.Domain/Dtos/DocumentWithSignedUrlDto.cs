namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DocumentWithSignedUrlDto : DocumentDto
    {
        public string PreSignedUrl { get; set; }
        public bool Close { get; set; }
        public bool IsPublic { get; set; }
    }
}
