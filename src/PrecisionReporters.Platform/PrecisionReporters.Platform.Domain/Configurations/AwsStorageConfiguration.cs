namespace PrecisionReporters.Platform.Domain.AppConfigurations.Sections
{
    public class AwsStorageConfiguration
    {
        public const string SectionName = "AwsStorageConfiguration";
        public string S3BucketRegion { get; set; }
    }
}
