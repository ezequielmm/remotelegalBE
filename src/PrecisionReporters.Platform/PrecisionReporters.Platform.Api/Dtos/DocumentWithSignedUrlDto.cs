namespace PrecisionReporters.Platform.Api.Dtos
{
    public class DocumentWithSignedUrlDto : DocumentDto
    {
        public string PreSignedUrl { get; set; }
        public bool Close { get; set; }
    }
}
