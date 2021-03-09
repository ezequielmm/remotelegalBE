using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Api.WebSockets;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private readonly IDocumentService _documentService;
        private readonly IMapper<Transcription, TranscriptionDto, object> _transcriptionMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;

        public TranscriptionsController(ITranscriptionsHandler transcriptionsHandler, ITranscriptionService transcriptionService,
            IMapper<Transcription, TranscriptionDto, object> transcriptionMapper, IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper, IDocumentService documentService)
        {
            _transcriptionsHandler = transcriptionsHandler;
            _transcriptionService = transcriptionService;
            _transcriptionMapper = transcriptionMapper;
            _documentMapper = documentMapper;
            _documentService = documentService;
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

        [HttpGet("{depositionId}/Files")]
        public async Task<ActionResult<List<DocumentDto>>> GetTranscrpitionsFiles(Guid depositionId)
        {
            var transcriptionsResult = await _transcriptionService.GetTranscriptionsFiles(depositionId);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            var transcriptionList = transcriptionsResult.Value.Select(d => _documentMapper.ToDto(d.Document));
            return Ok(transcriptionList);
        }

        /// <summary>
        /// Upload one or a set of transcriptions files and asociates them to a deposition
        /// </summary>
        /// <param name="depositionId">Identifier of the deposition which files are going to asociated with</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPost]
        [Route("{depositionId}/Files")]
        public async Task<IActionResult> UploadTranscriptionsFiles(Guid depositionId)
        {
            var files = FileHandlerHelper.GetFilesFromRequest(Request);

            if (files.Count == 0)
                return BadRequest("No files to upload");

            var uploadTranscriptionsFilesResult = await _documentService.UploadTranscriptions(depositionId, files);

            if (uploadTranscriptionsFilesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(uploadTranscriptionsFilesResult);

            return Ok();
        }

        [HttpGet("{depositionId}/offsets")]
        public async Task<ActionResult<List<TranscriptionTimeDto>>> GetTranscrpitionsTime(Guid depositionId)
        {
            var transcriptionsResult = await _transcriptionService.GetTranscriptionsWithTimeOffset(depositionId);
            if (transcriptionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(transcriptionsResult);

            return Ok(transcriptionsResult.Value);
        }
    }
}
