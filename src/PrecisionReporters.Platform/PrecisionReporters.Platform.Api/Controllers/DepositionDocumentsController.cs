using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Authorization.Attributes;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/Depositions")]
    [ApiController]
    [Authorize]
    public class DepositionDocumentsController : ControllerBase
    {
        private readonly IDepositionService _depositionService;
        private readonly IDocumentService _documentService;
        private readonly IDepositionDocumentService _depositionDocumentService;
        private readonly IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> _depositionDocumentMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;
        private readonly IMapper<Document, DocumentWithSignedUrlDto, object> _documentWithSignedUrlMapper;
        private readonly string _filePath;

        public DepositionDocumentsController(IDepositionService depositionService,
            IDocumentService documentService,
            IDepositionDocumentService depoistionDocumentService,
            IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> depositionDocumentMapper,
            IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper,
            IMapper<Document, DocumentWithSignedUrlDto, object> documentWithSignedUrlMapper,
            IWebHostEnvironment hostingEnvironment)
        {
            _depositionService = depositionService;
            _documentService = documentService;
            _depositionDocumentService = depoistionDocumentService;
            _depositionDocumentMapper = depositionDocumentMapper;
            _documentMapper = documentMapper;
            _documentWithSignedUrlMapper = documentWithSignedUrlMapper;
            _filePath = hostingEnvironment.ContentRootPath;
        }

        /// <summary>
        /// Gets the public url of a file. This url exipres after deposition end or after 2 hours if deposition doesn't have an end date
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Document information and a presigned url to the asociated file</returns>
        [HttpGet("{id}/SharedDocument")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult<DocumentWithSignedUrlDto>> GetSharedDocument([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var documentResult = await _depositionService.GetSharedDocument(id);
            if (documentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentResult);

            var document = documentResult.Value;
            var close = await _depositionDocumentService.ParticipantCanCloseDocument(document, id);

            var fileSignedUrlResult = _documentService.GetCannedPrivateURL(document);
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            var documentSignedDto = _documentWithSignedUrlMapper.ToDto(document);
            documentSignedDto.PreSignedUrl = fileSignedUrlResult.Value;
            documentSignedDto.Close = close;

            documentSignedDto.IsPublic = await _depositionDocumentService.IsPublicDocument(id, document.Id);

            return Ok(documentSignedDto);
        }

        /// <summary>
        /// Close and Save updates of a Stamped Document. 
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Response Status</returns>
        [HttpPost("{id}/SharedDocument/CloseStamped")]
        public async Task<ActionResult> CloseStampedDocument(Guid id, StampedDocumentDto stampedDocumentDto)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var documentResult = await _depositionService.GetSharedDocument(id);
            if (documentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentResult);

            var depositionDocumentDto = new DepositionDocumentDto
            {
                DocumentId = documentResult.Value.Id,
                DepositionId = id,
                StampLabel = stampedDocumentDto.StampLabel
            };

            var depositionDocument = _depositionDocumentMapper.ToModel(depositionDocumentDto);
            var temporalPath = Path.Combine(_filePath + ApplicationConstants.TemporalFileFolder);
            var depositionDocumentResult = await _depositionDocumentService.CloseStampedDepositionDocument(documentResult.Value, depositionDocument, identity, temporalPath);
            if (depositionDocumentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(depositionDocumentResult);

            return Ok();
        }        

        /// <summary>
        /// Close a not Stamped Document. 
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Response status</returns>
        [HttpPost("{id}/SharedDocument/Close")]
        public async Task<ActionResult> CloseDocument(Guid id)
        {
            var documentResult = await _depositionService.GetSharedDocument(id);
            if (documentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentResult);

            var result = await _depositionDocumentService.CloseDepositionDocument(documentResult.Value, id);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok();
        }

        /// <summary>
        /// Gets a list of the Entered Exhibits files.
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Document information of the Entered Exhibit for a given Deposition</returns>
        [HttpGet("{id}/EnteredExhibits")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult<List<DocumentDto>>> GetEnteredExhibits([ResourceId(ResourceType.Deposition)] Guid id, ExhibitSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var enteredExhibitsResult = await _depositionDocumentService.GetEnteredExhibits(id, sortedField, sortDirection);
            if (enteredExhibitsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(enteredExhibitsResult);

            var result = enteredExhibitsResult.Value.Select(d => GetDocumentWithStamp(d));
            return Ok(result);
        }        

        /// <summary>
        /// Gets the public url of a file. This url exipres after deposition end or after 2 hours if deposition doesn't have an end date
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Document information and a presigned url to the asociated file</returns>
        [HttpGet("{id}/Caption")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<DocumentWithSignedUrlDto>> GetDepositionCaption([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var documentResult = await _depositionService.GetDepositionCaption(id);
            if (documentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentResult);

            var document = documentResult.Value;

            var fileSignedUrlResult = _documentService.GetFileSignedUrl(document);
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            var documentSignedDto = _documentWithSignedUrlMapper.ToDto(document);
            documentSignedDto.PreSignedUrl = fileSignedUrlResult.Value;

            return Ok(documentSignedDto);
        }

        /// Shares a documents with all users in a deposition
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns>Ok if the process has completed successfully</returns>
        [HttpPut("{depositionId}/documents/{documentId}/Share")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<IActionResult> ShareEnteredExhibit([ResourceId(ResourceType.Deposition)] Guid depositionId, Guid documentId)
        {
            var documentsResult = await _documentService.ShareEnteredExhibit(depositionId, documentId);
            if (documentsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentsResult);
            return Ok();
        }

        /// <summary>
        /// Gets the public url of a file. This url exipres after deposition end or after 2 hours if deposition doesn't have an end date
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns></returns>
        [HttpGet("{depositionId}/documents/{documentId}/PreSignedUrl")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult<FileSignedDto>> GetFileSignedUrl([ResourceId(ResourceType.Deposition)] Guid depositionId, Guid documentId)
        {
            var fileSignedUrlResult = await _documentService.GetCannedPrivateURL(depositionId, documentId);
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            var fileSignedDto = new FileSignedDto
            {
                Url = fileSignedUrlResult.Value,
                IsPublic = true
            };

            return Ok(fileSignedDto);
        }

        /// <summary>
        /// Gets a list of the public files URLs. 
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns></returns>
        [HttpGet("{depositionId}/documents/PreSignedUrl")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult> GetFileSignedUrlList([ResourceId(ResourceType.Deposition)] Guid depositionId, [FromQuery] List<Guid> documentIds)
        {
            var fileSignedUrlListResult = await _documentService.GetFileSignedUrl(depositionId, documentIds);
            if (fileSignedUrlListResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlListResult);

            return Ok(new FileSignedListDto { URLs = fileSignedUrlListResult.Value });
        }

        /// <summary>
        /// Delete Transcription document from a deposition.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns>200 OK</returns>
        [HttpDelete("{depositionId}/transcripts/{documentId}")]
        [UserAuthorize(ResourceType.Document, ResourceAction.Delete)]
        public async Task<ActionResult> DeleteTranscript(Guid depositionId, [ResourceId(ResourceType.Document)] Guid documentId)
        {
            var documentsResult = await _depositionDocumentService.RemoveDepositionTranscript(depositionId, documentId);
            if (documentsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentsResult);
            return Ok();
        }

        /// <summary>
        /// Bring all deposition participants to the
        /// same page on the shared exhibit.
        /// </summary>
        /// <param name="depositionId"></param>
        /// <param name="bringAllToMe></param>
        /// <returns>200 OK</returns>
        [HttpPost("{depositionId}/BringAllToMe")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult> BringAllToMe([ResourceId(ResourceType.Deposition)] Guid depositionId, BringAllToMeDto bringAllToMe)
        {
            var result = await _depositionDocumentService.BringAllToMe(depositionId, bringAllToMe);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);
            return Ok();
        }

        private DocumentDto GetDocumentWithStamp(DepositionDocument depositionDocument)
        {
            var documentDto = _documentMapper.ToDto(depositionDocument.Document);
            documentDto.StampLabel = depositionDocument.StampLabel;
            return documentDto;
        }
    }
}
