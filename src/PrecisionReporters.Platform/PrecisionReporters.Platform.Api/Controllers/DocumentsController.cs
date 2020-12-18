using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

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
            if (Request.Form.Files.Count == 0)
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
            var depositionWithNewFilesResult = await _documentService.UploadDocuments(depositionId, identity, files);
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
    }
}
