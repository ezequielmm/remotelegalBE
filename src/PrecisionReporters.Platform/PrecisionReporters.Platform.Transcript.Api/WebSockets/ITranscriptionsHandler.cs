using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.WebSockets
{
    public interface ITranscriptionsHandler
    {
        Task HandleConnection(HttpContext context, WebSocket webSocket);
    }
}
