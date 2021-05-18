using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.WebSockets
{
    public class TranscriptionsHandler : ITranscriptionsHandler
    {
        private readonly ITranscriptionLiveService _transcriptionLiveService;
        private readonly Regex _jsonRegex = new Regex("(?:(?<b>{))(?<v>.*?)(?(p)%|})");

        public TranscriptionsHandler(ITranscriptionLiveService transcriptionLiveService)
        {
            _transcriptionLiveService = transcriptionLiveService;
        }

        public async Task HandleConnection(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 8];
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email);
            var depositionId = context.Request.Query["depositionId"];
            var sampleRate = int.Parse(context.Request.Query["sampleRate"]);
            var incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            await _transcriptionLiveService.InitializeRecognition(userEmail, depositionId, sampleRate);

            while (!incomingMessage.CloseStatus.HasValue)
            {
                if (!HandleJsonMessage(buffer))
                {
                    await _transcriptionLiveService.RecognizeAsync(buffer);
                }
                incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(incomingMessage.CloseStatus.Value, incomingMessage.CloseStatusDescription, CancellationToken.None);
        }

        private bool HandleJsonMessage(byte[] buffer)
        {
            var bufferFirstPosition = Encoding.UTF8.GetString(buffer, 0, 1);
            if (!bufferFirstPosition.StartsWith("{"))
                return false;

            var bufferString = Encoding.Default.GetString(buffer);
            var jsonResult = _jsonRegex.Match(bufferString).Value;
            try
            {
                var webSocketData = JsonConvert.DeserializeObject<WebSocketDto>(jsonResult);

                if (webSocketData == null)
                    return false;

                if (webSocketData.OffRecord)
                    _transcriptionLiveService.StopTranscriptStream();

                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }
}
