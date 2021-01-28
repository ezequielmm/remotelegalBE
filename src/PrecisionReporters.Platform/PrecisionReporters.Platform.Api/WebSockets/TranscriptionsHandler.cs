using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
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
        private readonly ITranscriptionLiveService _transcriptionService;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;

        public TranscriptionsHandler(ITranscriptionLiveService transcriptionService, IMapper<Transcription, TranscriptionDto, object> transcriptionMapper)
        {
            _transcriptionService = transcriptionService;
            _transcriptionMapper = transcriptionMapper;
        }

        public async Task HandleConnection(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 8];
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email);
            var depositionId = context.Request.Query["depositionId"];
            var sampleRate = Int32.Parse(context.Request.Query["sampleRate"]);
            var incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!incomingMessage.CloseStatus.HasValue)
            {
                var transcription = await _transcriptionService.RecognizeAsync(buffer, userEmail, depositionId, sampleRate);
                if (!string.IsNullOrWhiteSpace(transcription.Text))
                {
                    var transcriptionDto = _transcriptionMapper.ToDto(transcription);
                    var message = JsonConvert.SerializeObject(transcriptionDto, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                }

                incomingMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(incomingMessage.CloseStatus.Value, incomingMessage.CloseStatusDescription, CancellationToken.None);
        }
    }
}
