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

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly IUserService _userService;
        private readonly IMapper<Case, CaseDto, CreateCaseDto> _caseMapper;

        public CasesController(ICaseService caseService,
            IMapper<Case, CaseDto, CreateCaseDto> caseMapper,
            IUserService userService)
        {
            _caseService = caseService;
            _caseMapper = caseMapper;
            _userService = userService;
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

            var createdCase = _caseMapper.ToDto(createCaseResult.Value);
            return Ok(createdCase);
        }

        /// <summary>
        /// Gets a Case based in its Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Case with the given Id</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CaseDto>> GetCaseById(Guid id)
        {
            var findCaseResult = await _caseService.GetCaseById(id);
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
        public async Task<ActionResult<List<CaseDto>>> GetCasesForCurrentUser(CaseSortField? sortedField  = null, SortDirection? sortDirection = null)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);

            var getCasesResult = await _caseService.GetCasesForUser(userEmail, sortedField, sortDirection);
            if (getCasesResult.IsFailed)
                return WebApiResponses.GetErrorResponse(getCasesResult);

            return Ok(getCasesResult.Value.Select(c => _caseMapper.ToDto(c)));
        }
    }
}
