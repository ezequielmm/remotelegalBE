﻿using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.WebSockets
{
    public interface ITranscriptionsHandler
    {
        Task HandleConnection(HttpContext context, WebSocket webSocket);
    }
}