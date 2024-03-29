﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;

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
        private readonly IMapper<Case, EditCaseDto, object> _editCaseMapper;

        public CasesController(ICaseService caseService,
            IMapper<Case, CaseDto, CreateCaseDto> caseMapper,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper,
            IMapper<Case, EditCaseDto, object> editCaseMapper)
        {
            _caseService = caseService;
            _caseMapper = caseMapper;
            _depositionMapper = depositionMapper;
            _editCaseMapper = editCaseMapper;
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
        public async Task<ActionResult<CaseWithDepositionsDto>> ScheduleDepositions([ResourceId(ResourceType.Case)] Guid id, CasePatchDto casePatchDto)
        {
            if (casePatchDto.Depositions == null)
                return BadRequest("Depositions missing");

            var files = FileHandlerHelper.GetFilesFromRequest(Request.Form.Files);

            var getCasesResult = await _caseService.ScheduleDepositions(id, casePatchDto.Depositions.Select(d => _depositionMapper.ToModel(d)), files);

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

        /// <summary>
        /// Edit an existing case details
        /// </summary>
        /// <param name="id">Case identifier</param>
        /// <param name="editCaseDto">Edited case details</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPut("{id}")]
        [UserAuthorize(ResourceType.Case, ResourceAction.Update)]
        public async Task<ActionResult<CaseDto>> EditCase([ResourceId(ResourceType.Case)] Guid id, EditCaseDto editCaseDto)
        {
            var caseToEdit = _editCaseMapper.ToModel(editCaseDto);
            caseToEdit.Id = id;
            var editCaseResult = await _caseService.EditCase(caseToEdit);

            if (editCaseResult.IsFailed)
                return WebApiResponses.GetErrorResponse(editCaseResult);

            return Ok(_caseMapper.ToDto(editCaseResult.Value));
        }
    }
}
