using Amazon.SimpleEmail.Model;
using PrecisionReporters.Platform.Domain.Commons;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsEmailService
    {
        Task<SendBulkTemplatedEmailResponse> SendEmailAsync(List<BulkEmailDestination> destinations, string templateName);
        Task SetTemplateEmailRequest(EmailTemplateInfo emailData);
    }
}
