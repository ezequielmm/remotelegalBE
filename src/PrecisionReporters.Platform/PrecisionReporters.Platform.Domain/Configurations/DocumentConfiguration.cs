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
        public int PreSignedUploadUrlValidSeconds { get; set; }
        public long MaxRequestBodySize { get; set; }
        public string PostDepoVideoBucket { get; set; }
        public string EnvironmentFilesBucket { get; set; }
        public IReadOnlyList<string> AcceptedTranscriptionExtensions { get; set; }
        public string FrontEndContentBucket { get; set; }
        public string PDFTronLicenseKey { get; set; }
        public string CloudfrontPrivateKey { get; set; }
        public string CloudfrontXmlKey { get; set; }
        public string CloudfrontPolicyStatement { get; set; }
        public IReadOnlyList<string> NonConvertToPdfExtensions { get; set; }
        public string UseSignatureVersion4 { get; set; }
    }
}