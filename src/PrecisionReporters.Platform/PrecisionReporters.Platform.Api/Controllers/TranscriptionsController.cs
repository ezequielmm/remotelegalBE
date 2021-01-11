using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Api.WebSockets;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class TranscriptionsController : Controller
    {
        private readonly ITranscriptionsHandler _transcriptionsHandler;
        private readonly ITranscriptionService _transcriptionService;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;

        public TranscriptionsController(ITranscriptionsHandler transcriptionsHandler, ITranscriptionService transcriptionService,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper)
        {
            _transcriptionsHandler = transcriptionsHandler;
            _transcriptionService = transcriptionService;
            _transcriptionMapper = transcriptionMapper;
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

        [HttpGet("{depositionId}")]
        public async Task<ActionResult<List<TranscriptionDto>>> GetTranscrpitions(Guid depositionId)
        {
            var transcriptionsResult = await _transcriptionService.GetTranscriptionsByDepositionId(depositionId);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            var transcriptionList = transcriptionsResult.Value.Select(t => _transcriptionMapper.ToDto(t));
            return Ok(transcriptionList);            
        }
    }
}
