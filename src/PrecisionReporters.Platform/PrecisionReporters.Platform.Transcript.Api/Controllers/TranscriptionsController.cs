using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class TranscriptionsController : Controller
    {
        private readonly ITranscriptionService _transcriptionService;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;

        public TranscriptionsController(ITranscriptionService transcriptionService,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper, IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper)
        {
            _transcriptionService = transcriptionService;
            _transcriptionMapper = transcriptionMapper;
            _documentMapper = documentMapper;
        }

        [HttpGet("{depositionId}")]
        public async Task<ActionResult<List<TranscriptionDto>>> GetTranscriptions(Guid depositionId)
        {
            // TODO: review authorization

            var transcriptionsResult = await _transcriptionService.GetTranscriptionsByDepositionId(depositionId);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            var transcriptionList = transcriptionsResult.Value.Select(t => _transcriptionMapper.ToDto(t));
            return Ok(transcriptionList);
        }

        [HttpGet("{depositionId}/Files")]
        public async Task<ActionResult<List<DocumentDto>>> GetTranscriptionsFiles(Guid depositionId)
        {
            // TODO: review authorization

            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var transcriptionsResult = await _transcriptionService.GetTranscriptionsFiles(depositionId, identity);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            var transcriptionList = transcriptionsResult.Value.Select(d => _documentMapper.ToDto(d.Document));
            return Ok(transcriptionList);
        }

        [HttpGet("{depositionId}/offsets")]
        public async Task<ActionResult<List<TranscriptionTimeDto>>> GetTranscriptionsTime(Guid depositionId)
        {
            // TODO: review authorization

            var transcriptionsResult = await _transcriptionService.GetTranscriptionsWithTimeOffset(depositionId);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            return Ok(transcriptionsResult.Value);
        }
    }
}
