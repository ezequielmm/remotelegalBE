using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Authorization.Attributes;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]

    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;

        public DocumentsController(IDocumentService documentService, IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper)
        {
            _documentService = documentService;
            _documentMapper = documentMapper;
        }

        /// <summary>
        /// Upload one or a set of files and asociates them to a deposition
        /// </summary>
        /// <param name="depositionId">Identifier of the deposition which files are going to asociated with</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPost]
        [Route("Depositions/{depositionId}/Exhibits")]
        public async Task<IActionResult> UploadFiles(Guid depositionId)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest("No files to upload");

            var files = new List<FileTransferInfo>();
            foreach (var file in Request.Form.Files)
            {
                var fileTransferInfo = new FileTransferInfo
                {
                    FileStream = file.OpenReadStream(),
                    Name = file.FileName,
                    Length = file.Length
                };
                files.Add(fileTransferInfo);
            }
            var folder = DocumentType.Exhibit.GetDescription();
            var depositionWithNewFilesResult = await _documentService.UploadDocuments(depositionId, identity, files, folder, DocumentType.Exhibit);
            if (depositionWithNewFilesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(depositionWithNewFilesResult);

            return Ok();
        }

        /// <summary>
        /// Gets a list of documents from a deposition uploaded by the user, or documents shared with the user
        /// </summary>
        /// <param name="depositionId"></param>
        /// <returns>List of documents information</returns>
        [HttpGet("Depositions/{depositionId}/MyExhibits")]
        public async Task<ActionResult<List<DocumentDto>>> GetMyExhibits(Guid depositionId)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var documentsResult = await _documentService.GetExhibitsForUser(depositionId, identity);
            if (documentsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentsResult);
            return Ok(documentsResult.Value.Select(d => _documentMapper.ToDto(d)));
        }

        /// <summary>
        /// Gets the public url of a file. This url exipres after deposition end or after 2 hours if deposition doesn't have an end date
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns></returns>
        [HttpGet("[controller]/{id}/PreSignedUrl")]
        [UserAuthorize(ResourceType.Document, ResourceAction.View)]
        public async Task<ActionResult<FileSignedDto>> GetFileSignedUrl([ResourceId(ResourceType.Document)] Guid id)
        {
            var fileSignedUrlResult = await _documentService.GetFileSignedUrl(id);
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            var fileSignedDto = new FileSignedDto
            {
                Url = fileSignedUrlResult.Value,
                IsPublic = false
            };

            return Ok(fileSignedDto);
        }

        /// Shares a documents with all users in a deposition
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns>Ok if the process has completed successfully</returns>
        [HttpPut("[controller]/{id}/Share")]
        [UserAuthorize(ResourceType.Document, ResourceAction.Update)]
        public async Task<IActionResult> ShareDocument([ResourceId(ResourceType.Document)] Guid id)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var documentsResult = await _documentService.Share(id, identity);
            if (documentsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentsResult);
            return Ok();
        }

        /// Gets details of a given document
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Detailed document information</returns>
        [HttpGet("[controller]/{id}")]
        [UserAuthorize(ResourceType.Document, ResourceAction.View)]
        public async Task<ActionResult<DocumentDto>> GetDocument([ResourceId(ResourceType.Document)] Guid id)
        {
            var documentResult = await _documentService.GetDocument(id);
            if (documentResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentResult);

            return Ok(_documentMapper.ToDto(documentResult.Value));
        }

        /// <summary>
        /// Delete My Exhibit document from a deposition.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns>List of documents information</returns>
        [HttpDelete("Depositions/{depositionId}/documents/{documentId}")]
        [UserAuthorize(ResourceType.Document, ResourceAction.Delete)]
        public async Task<ActionResult<List<DocumentDto>>> DeleteMyExhibits(Guid depositionId, [ResourceId(ResourceType.Document)] Guid documentId)
        {
            var documentsResult = await _documentService.RemoveDepositionDocument(depositionId, documentId);
            if (documentsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentsResult);

            return Ok();
        }

        /// <summary>
        /// Gets the public urls of the FrontEnd content. This url exipres after 2 hours
        /// </summary>
        /// <returns></returns>
        [HttpGet("[controller]/FrontendContent")]
        [AllowAnonymous]
        public async Task<ActionResult<List<FileSignedDto>>> FrontendContent()
        {
            var fileSignedUrlResult = await _documentService.GetFrontEndContent();
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            return Ok(fileSignedUrlResult.Value);
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
            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest("No files to upload");

            var files = FileHandlerHelper.GetFilesFromRequest(Request);

            var uploadTranscriptionsFilesResult = await _documentService.UploadTranscriptions(depositionId, files);

            if (uploadTranscriptionsFilesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(uploadTranscriptionsFilesResult);

            return Ok();
        }
    }
}
