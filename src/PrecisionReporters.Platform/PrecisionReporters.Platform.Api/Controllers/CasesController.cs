using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Api.Authorization.Attributes;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly IMapper<Case, CaseDto, CreateCaseDto> _caseMapper;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;

        public CasesController(ICaseService caseService, IMapper<Case, CaseDto, CreateCaseDto> caseMapper, IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper)
        {
            _caseService = caseService;
            _caseMapper = caseMapper;
            _depositionMapper = depositionMapper;
        }

        /// <summary>
        /// Creates a new case with a given name
        /// </summary>
        /// <param name="caseDto"></param>
        /// <returns>Newly created case</returns>
        [HttpPost]
        public async Task<ActionResult<CaseDto>> CreateCase(CreateCaseDto caseDto)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var caseModel = _caseMapper.ToModel(caseDto);
            var createCaseResult = await _caseService.CreateCase(userEmail, caseModel);
            if (createCaseResult.IsFailed)
                return WebApiResponses.GetErrorResponse(createCaseResult);

            var createdCase = _caseMapper.ToDto(createCaseResult.Value);
            return Ok(createdCase);
        }

        /// <summary>
        /// Gets a Case based in its Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Case with the given Id</returns>
        [HttpGet("{id}")]
        [UserAuthorize(ResourceType.Case, ResourceAction.View)]
        public async Task<ActionResult<CaseDto>> GetCaseById([ResourceId(ResourceType.Case)] Guid id)
        {
            var findCaseResult = await _caseService.GetCaseById(id, new[] { nameof(Case.Members), nameof(Case.Depositions), nameof(Case.AddedBy) });
            if (findCaseResult.IsFailed)
                return WebApiResponses.GetErrorResponse(findCaseResult);

            var foundCase = _caseMapper.ToDto(findCaseResult.Value);
            return Ok(foundCase);
        }

        /// <summary>
        /// Gets all cases
        /// </summary>
        /// <returns>A list with all cases</returns>
        [HttpGet]
        public async Task<ActionResult<List<CaseDto>>> GetCasesForCurrentUser(CaseSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);

            var getCasesResult = await _caseService.GetCasesForUser(userEmail, sortedField, sortDirection);
            if (getCasesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getCasesResult);

            return Ok(getCasesResult.Value.Select(c => _caseMapper.ToDto(c)));
        }

        /// <summary>
        /// Adds new depositions to a case
        /// </summary>
        /// <param name="id"></param>
        /// <param name="casePatchDto"></param>
        /// <returns>Updated case with the scheduled depositions</returns>
        [HttpPatch("{id}")]
        public async Task<ActionResult<CaseWithDepositionsDto>> ScheduleDepositions(Guid id, CasePatchDto casePatchDto)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);

            if (casePatchDto.Depositions == null)
                return BadRequest("Depositions missing");

            var files = new Dictionary<string, FileTransferInfo>();
            foreach (var file in Request.Form.Files)
            {
                var fileTransferInfo = new FileTransferInfo
                {
                    FileStream = file.OpenReadStream(),
                    Name = file.FileName,
                    Length = file.Length
                };
                files.Add(file.Name,fileTransferInfo);
            }

            var getCasesResult = await _caseService.ScheduleDepositions(userEmail, id, casePatchDto.Depositions.Select(d => _depositionMapper.ToModel(d)), files);
            if (getCasesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getCasesResult);

            var caseToUpdate = getCasesResult.Value;
            var caseWithDepositions = new CaseWithDepositionsDto
            {
                Id = caseToUpdate.Id,
                CreationDate = caseToUpdate.CreationDate,
                Name = caseToUpdate.Name,
                CaseNumber = caseToUpdate.CaseNumber,
                AddedById = caseToUpdate.AddedById,
                Depositions = caseToUpdate.Depositions.Select(d => _depositionMapper.ToDto(d)).ToList()
            };
            return new ObjectResult(caseWithDepositions);
        }
    }
}
