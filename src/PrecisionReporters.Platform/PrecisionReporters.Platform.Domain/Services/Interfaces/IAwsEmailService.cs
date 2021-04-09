using Amazon.SimpleEmail.Model;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsEmailService
    {
        Task<SendBulkTemplatedEmailResponse> SendEmailAsync(List<BulkEmailDestination> destinations, string templateName, string sender = null);
        Task SetTemplateEmailRequest(EmailTemplateInfo emailData, string sender = null);
        Task SendRawEmailNotification(MemoryStream streamMessage);
        Task SendRawEmailNotification(Deposition deposition);
        Task SendRawEmailNotification(Deposition deposition, Participant participant);
    }
}
