using Newtonsoft.Json;

namespace PrecisionReporters.Platform.Data.Entities
{
    //TODO Change JsonProperty after FE - Participant Component refactoring
    public class TwilioIdentity
    {
        [JsonProperty("n")]
        public string FirstName { get; set; }
        [JsonProperty("l")]
        public string LastName { get; set; }
        [JsonProperty("r")]
        public int Role { get; set; }
        [JsonProperty("e")]
        public string Email { get; set; }
        [JsonProperty("a")]
        public int IsAdmin { get; set; }
    }
}
