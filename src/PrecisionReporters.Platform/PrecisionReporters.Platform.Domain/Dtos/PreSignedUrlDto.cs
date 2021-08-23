using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class PreSignedUrlDto
    {
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
