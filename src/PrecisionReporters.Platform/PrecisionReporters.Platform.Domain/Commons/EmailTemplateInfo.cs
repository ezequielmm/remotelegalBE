using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Commons
{
    public class EmailTemplateInfo
    {
        public List<string> EmailTo { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, string> TemplateData { get; set; }              
    }
}
