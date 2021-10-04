using System.Diagnostics;
using Amazon.SimpleNotificationService.Util;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class MessageSignatureHandler : HandlerBase<Message>, IMessageSignatureHandler
    {
        private readonly IAwsSnsWrapper _awsSnsWrapper;
        public MessageSignatureHandler(IAwsSnsWrapper awsSnsWrapper)
        {
            _awsSnsWrapper = awsSnsWrapper;
        }
        public override async Task HandleRequest(Message message)
        {
            if (_awsSnsWrapper.IsMessageSignatureValid(message))
            {
                await successor.HandleRequest(message);
            }
            else
            {
                var msg = $"Invalid Message Signature: {message.Signature}";
                throw new NotificationHandlerException(msg);
            }
        }
    }
}
