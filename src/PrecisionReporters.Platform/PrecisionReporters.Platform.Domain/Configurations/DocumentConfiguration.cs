using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class DocumentConfiguration
    {
        public const string SectionName = "DocumentConfigurations";

        public string BucketName { get; set; }
        public long MaxFileSize { get; set; }
        public IReadOnlyList<string> AcceptedFileExtensions { get; set; }
        public int PreSignedUrlValidHours { get; set; }
        public long MaxRequestBodySize { get; set; }
        public string PostDepoVideoBucket { get; set; }
        public string EnvironmentFilesBucket { get; set; }
        public IReadOnlyList<string> AcceptedTranscriptionExtensions { get; set; }
    }
}