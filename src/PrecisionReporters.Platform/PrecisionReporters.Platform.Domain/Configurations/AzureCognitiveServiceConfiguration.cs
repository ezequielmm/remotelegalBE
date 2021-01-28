namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class AzureCognitiveServiceConfiguration
    {
        public const string SectionName = nameof(AzureCognitiveServiceConfiguration);
        public string SubscriptionKey { get; set; }
        public string RegionCode { get; set; }
    }
}
