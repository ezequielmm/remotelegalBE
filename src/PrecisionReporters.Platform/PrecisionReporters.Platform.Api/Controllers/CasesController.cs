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
            var newCase = await _caseService.CreateCase(userEmail, caseModel);

            return Ok(_caseMapper.ToDto(newCase));
        }

        /// <summary>
        /// Gets a Case based in its Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Case with the given Id</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CaseDto>> GetCaseById(Guid id)
        {
            var model = await _caseService.GetCaseById(id);
            return Ok(_caseMapper.ToDto(model));
        }

        /// <summary>
        /// Gets all cases
        /// </summary>
        /// <returns>A list with all cases</returns>
        [HttpGet]
        public async Task<ActionResult<List<Case>>> GetCasesForCurrentUser()
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var cases = await _caseService.GetCasesForUser(userEmail);
            return Ok(cases.Select(c => _caseMapper.ToDto(c)));
        }
    }
}
