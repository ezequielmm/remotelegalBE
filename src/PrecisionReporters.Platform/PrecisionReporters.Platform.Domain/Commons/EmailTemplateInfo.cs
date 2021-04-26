using Ical.Net;
using Ical.Net.CalendarComponents;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Commons
{
    public class EmailTemplateInfo
    {
        public List<string> EmailTo { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, string> TemplateData { get; set; }   
        public Calendar Calendar { get; set; }
        public string Subject { get; set; }
        public Dictionary<string, string> SubjectData { get; set; }
        public string AddiotionalText { get; set; }
    }
}
