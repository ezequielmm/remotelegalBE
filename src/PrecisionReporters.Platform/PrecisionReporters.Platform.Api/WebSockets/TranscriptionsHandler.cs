using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.WebSockets
{
    public class TranscriptionsHandler : ITranscriptionsHandler
    {
        private readonly ITranscriptionService _transcriptionService;

        public TranscriptionsHandler(ITranscriptionService transcriptionService)
        {
            _transcriptionService = transcriptionService;
        }

        public async Task HandleConnection(HttpContext context, WebSocket webSocket)
        {          
            var buffer = new byte[1024 * 8];
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email);
            var depositionId = context.Request.Query["depositionId"];
            var incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            while (!incomingMessage.CloseStatus.HasValue)
            {
                var transcription = await _transcriptionService.RecognizeAsync(buffer, userEmail, depositionId);
                if (!string.IsNullOrWhiteSpace(transcription.Transcript))
                {                    
                    var message = JsonConvert.SerializeObject(new { text = transcription.Transcript, date = transcription.TimeOffset });
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                }

                incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            }
            await webSocket.CloseAsync(incomingMessage.CloseStatus.Value, incomingMessage.CloseStatusDescription, CancellationToken.None);
        }
    }
}
