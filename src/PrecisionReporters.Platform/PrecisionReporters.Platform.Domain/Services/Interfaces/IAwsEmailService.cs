using Amazon.SimpleEmail.Model;
using PrecisionReporters.Platform.Domain.Commons;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsEmailService
    {
        Task<SendRawEmailResponse> SendEmailAsync(EmailTemplateInfo emailDataHelper, string emailTo, List<string> cc = null, List<string> bcc = null);
    }
}
