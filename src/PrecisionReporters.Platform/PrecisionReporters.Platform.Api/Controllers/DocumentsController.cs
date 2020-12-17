using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Helpers;
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

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
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
    }
}
