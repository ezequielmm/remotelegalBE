using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ISnsNotificationService _snsNotificationService;

        public NotificationsController(ISnsNotificationService snsNotificationService)
        {
            _snsNotificationService = snsNotificationService;
        }

        [HttpPost]
        [Route("Notifications/SnsCallback")]
        public async Task<IActionResult> SnsCallback()
        {
            var notificationStatus = await _snsNotificationService.Notify(Request.Body);

            if (notificationStatus.IsFailed)
                return WebApiResponses.GetErrorResponse(notificationStatus);

            return Ok();
        }
    }
}
