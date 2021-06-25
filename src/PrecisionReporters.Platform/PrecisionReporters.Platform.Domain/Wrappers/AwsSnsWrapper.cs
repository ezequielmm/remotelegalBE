using Amazon.SimpleNotificationService.Util;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Wrappers
{
    public class AwsSnsWrapper : IAwsSnsWrapper
    {
        public bool IsMessageSignatureValid(Message message)
        {
            return message.IsMessageSignatureValid();
        }

        public Message ParseMessage(string content)
        {
            return Message.ParseMessage(content);
        }
    }
}
