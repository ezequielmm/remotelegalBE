using Amazon.SimpleNotificationService.Util;

namespace PrecisionReporters.Platform.Domain.Wrappers.Interfaces
{
    public interface IAwsSnsWrapper
    {
        public bool IsMessageSignatureValid(Message message);
        public Message ParseMessage(string content);
    }
}
