﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Authorization.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Attributes;

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

        public DepositionDocumentsController(IDepositionService depositionService,
            IDocumentService documentService,
            IDepositionDocumentService depoistionDocumentService,
            IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> depositionDocumentMapper,
            IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper,
            IMapper<Document, DocumentWithSignedUrlDto, object> documentWithSignedUrlMapper)
        {
            _depositionService = depositionService;
            _documentService = documentService;
            _depositionDocumentService = depoistionDocumentService;
            _depositionDocumentMapper = depositionDocumentMapper;
            _documentMapper = documentMapper;
            _documentWithSignedUrlMapper = documentWithSignedUrlMapper;
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

            var fileSignedUrlResult = _documentService.GetFileSignedUrl(document);
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

            if (Request.Form.Files.Count == 0)
                return BadRequest("No files to update.");

            var file = Request.Form.Files[0];
            var fileTransferInfo = new FileTransferInfo
            {
                FileStream = file.OpenReadStream(),
                Name = documentResult.Value.DisplayName,
                Length = file.Length
            };

            var depositionDocument = _depositionDocumentMapper.ToModel(depositionDocumentDto);
            var depositionDocumentResult = await _depositionDocumentService.CloseStampedDepositionDocument(documentResult.Value, depositionDocument, identity, fileTransferInfo);
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

        private DocumentDto GetDocumentWithStamp(DepositionDocument depositionDocument)
        {
            var documentDto = _documentMapper.ToDto(depositionDocument.Document);
            documentDto.StampLabel = depositionDocument.StampLabel;
            return documentDto;
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
            var fileSignedUrlResult = await _documentService.GetFileSignedUrl(depositionId, documentId);
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
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult> GetFileSignedUrlList([ResourceId(ResourceType.Deposition)] Guid depositionId, DocumentIdListDto documentIdListDto)
        {
            var fileSignedUrlListResult = await _documentService.GetFileSignedUrl(depositionId, documentIdListDto);
            if (fileSignedUrlListResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlListResult);

            return Ok(new FileSignedListDto { URLs = fileSignedUrlListResult.Value});
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
    }
}
