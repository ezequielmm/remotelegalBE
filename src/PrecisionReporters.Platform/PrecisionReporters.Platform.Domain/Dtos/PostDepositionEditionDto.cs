namespace PrecisionReporters.Platform.Data.Entities
{
    public class PostDepositionEditionDto
    {
        private const string _successMessage = "notify-post-depo-complete";

        public string ConfigurationId { get; set; }
        public string Video { get; set; }

        public string GetCompositionId()
        {
            return Video.Split(new char[] { '.' })[0];
        }

        public bool IsComplete()
        {
            return ConfigurationId.Contains(_successMessage);
        }
    }
}
