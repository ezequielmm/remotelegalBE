namespace PrecisionReporters.Platform.Api.AppConfigurations.Sections
{
    public class AwsStorageConfiguration
    {
        public const string SectionName = "AwsStorageConfiguration";
        public string S3DestinationKey { get; set; }
        public string S3DestinationSecret { get; set; }
        public string S3BucketRegion { get; set; }
    }
}
