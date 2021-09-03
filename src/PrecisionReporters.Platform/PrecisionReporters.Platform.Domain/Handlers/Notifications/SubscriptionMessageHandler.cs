using Amazon.SimpleNotificationService.Util;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class SubscriptionMessageHandler : HandlerBase<Message>, ISubscriptionMessageHandler
    {
        private readonly ISnsHelper _snsHelper;

        public SubscriptionMessageHandler(ISnsHelper snsHelper)
        {
            _snsHelper = snsHelper;
        }
        public override async Task HandleRequest(Message message)
        {
            if (message.IsSubscriptionType)
            {
                var result = await _snsHelper.SubscribeEndpoint(message.SubscribeURL);
                if (result.IsFailed)
                {
                    var msg = $"There was an error subscribing URL, {result}";
                    throw new NotificationHandlerException(msg);
                }
            }
            else
                await successor.HandleRequest(message);
        }
    }
}
