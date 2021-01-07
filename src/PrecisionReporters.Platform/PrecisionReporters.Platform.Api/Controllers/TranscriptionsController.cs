using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.WebSockets;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class TranscriptionsController : Controller
    {
        private readonly ITranscriptionsHandler _transcriptionsHandler;

        public TranscriptionsController(ITranscriptionsHandler transcriptionsHandler)
        {
            _transcriptionsHandler = transcriptionsHandler;
        }

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _transcriptionsHandler.HandleConnection(HttpContext, webSocket);
            }
        }
    }
}
