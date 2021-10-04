using Amazon.SimpleNotificationService.Util;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Wrappers
{
    /// <summary>
    /// AWS sns Wrapper for local use
    /// </summary>
    public class AwsSnsWrapperLocal : IAwsSnsWrapper
    {
        public bool IsMessageSignatureValid(Message message)
        {
            return true;
        }

        public Message ParseMessage(string content)
        {
            return Message.ParseMessage(content);
        }
    }
}