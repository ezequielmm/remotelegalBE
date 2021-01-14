using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PrecisionReporters.Platform.Api.Authorization.Attributes;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepositionsController : ControllerBase
    {
        private readonly IDepositionService _depositionService;
        private readonly IDocumentService _documentService;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;

        public DepositionsController(IDepositionService depositionService,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper,
            IDocumentService documentService)
        {
            _depositionService = depositionService;
            _depositionMapper = depositionMapper;
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<ActionResult<List<DepositionDto>>> GetDepositions(DepositionStatus? status, DepositionSortField? sortedField,
            SortDirection? sortDirection)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var depositions = await _depositionService.GetDepositionsByStatus(status, sortedField, sortDirection, userEmail);

            return Ok(depositions.Select(c => _depositionMapper.ToDto(c)));
        }

        // <summary>
        /// Join to an existing Deposition
        /// </summary>
        /// <param name="id">DepositionId to Join.</param>
        /// <returns>JoinDepositionDto object.</returns>
        [HttpPost("{id}/join")]
        public async Task<ActionResult<JoinDepositionDto>> JoinDeposition(Guid id)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var joinDepositionInfoResult = await _depositionService.JoinDeposition(id, identity);

            return Ok(joinDepositionInfoResult.Value);
        }

        // <summary>
        /// End an existing Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to End.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpPost("{id}/end")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.EndDeposition)]
        public async Task<ActionResult<DepositionDto>> EndDeposition([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var endDepositionResult = await _depositionService.EndDeposition(id);
            if (endDepositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(endDepositionResult);

            return Ok(_depositionMapper.ToDto(endDepositionResult.Value));
        }

        // <summary>
        /// Get an existing Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to End.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DepositionDto>> GetDeposition(Guid id)
        {
            var endDepositionResult = await _depositionService.GetDepositionById(id);
            if (endDepositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(endDepositionResult);

            return Ok(_depositionMapper.ToDto(endDepositionResult.Value));
        }

        // <summary>
        /// Change On Record status to a Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to add the OnRecord / OffRecord event.</param>
        /// <param name="onTheRecord">Status OnTheRecord / OffTheRecord event.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpPost("{id}/record")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Recording)]
        public async Task<ActionResult<DepositionDto>> DepositionRecord([ResourceId(ResourceType.Deposition)] Guid id, [FromQuery, BindRequired] bool onTheRecord)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var goOnTheRecordResult = await _depositionService.GoOnTheRecord(id, onTheRecord, userEmail);
            if (goOnTheRecordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(goOnTheRecordResult);

            return Ok(_depositionMapper.ToDto(goOnTheRecordResult.Value));
        }

        /// <summary>
        /// Gets the public url of a file. This url exipres after deposition end or after 2 hours if deposition doesn't have an end date
        /// </summary>
        /// <param name="id">Document identifier</param>
        /// <returns>Document information and a presigned url to the asociated file</returns>
        [HttpGet("{id}/SharedDocument")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult<DocumentWithSignedUrlDto>> GetSharedDocument([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var documentIdResult = await _depositionService.GetSharedDocument(id);
            if (documentIdResult.IsFailed)
                return WebApiResponses.GetErrorResponse(documentIdResult);

            var document = documentIdResult.Value;

            var fileSignedUrlResult = _documentService.GetFileSignedUrl(document);
            if (fileSignedUrlResult.IsFailed)
                return WebApiResponses.GetErrorResponse(fileSignedUrlResult);

            return Ok(new DocumentWithSignedUrlDto
            {
                Id = document.Id,
                CreationDate = document.CreationDate,
                DisplayName = document.DisplayName,
                Size = document.Size,
                Name = document.Name,
                PreSignedUrl = fileSignedUrlResult.Value,
                AddedBy = new UserOutputDto
                {
                    Id = document.AddedBy.Id,
                    FirstName = document.AddedBy.FirstName,
                    LastName = document.AddedBy.LastName
                }
            });
        }
    }
}
