using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Net.WebSockets;
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

            var incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            while (!incomingMessage.CloseStatus.HasValue)
            {
                var transcript = await _transcriptionService.RecognizeAsync(buffer);
                if (!string.IsNullOrWhiteSpace(transcript))
                {                    
                    var message = JsonConvert.SerializeObject(new { text = transcript });
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                }

                incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            }
            await webSocket.CloseAsync(incomingMessage.CloseStatus.Value, incomingMessage.CloseStatusDescription, CancellationToken.None);
        }
    }
}
