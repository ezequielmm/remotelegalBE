using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CaseController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly IMapper<Case, CaseDto, CreateCaseDto> _caseMapper;

        public CaseController(ICaseService caseService,
            IMapper<Case, CaseDto, CreateCaseDto> caseMapper)
        {
            _caseService = caseService;
            _caseMapper = caseMapper;
        }

        /// <summary>
        /// Creates a new case with a given name
        /// </summary>
        /// <param name="caseDto"></param>
        /// <returns>Newly created case</returns>
        [HttpPost]
        public async Task<ActionResult<CaseDto>> CreateCase(CreateCaseDto caseDto)
        {
            var model = _caseMapper.ToModel(caseDto);
            var newCase = await _caseService.CreateCase(model);

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
        public async Task<ActionResult<IEnumerable<CaseDto>>> GetCases()
        {
            var cases = await _caseService.GetCases();
            return Ok(cases.Select(c => _caseMapper.ToDto(c)));
        }
    }
}
