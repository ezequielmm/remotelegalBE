﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepositionsController : ControllerBase
    {
        private readonly IDepositionService _depositionService;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;
        private readonly IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto> _depositionEventMapper;

        public DepositionsController(IDepositionService depositionService,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper,
            IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto> depositionEventMapper)
        {
            _depositionService = depositionService;
            _depositionMapper = depositionMapper;
            _depositionEventMapper = depositionEventMapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<DepositionDto>>> GetDepositions(DepositionStatus? status, DepositionSortField? sortedField,
            SortDirection? sortDirection)
        {
            var depositions = await _depositionService.GetDepositionsByStatus(status, sortedField, sortDirection);
            
            return Ok(depositions.Select(c => _depositionMapper.ToDto(c)));
        }

        // <summary>
        /// Join to an existing Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to Join.</param>
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
        public async Task<ActionResult<DepositionDto>> EndDeposition(Guid id)
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
        public async Task<ActionResult<DepositionDto>> DepositionRecord(Guid id, [FromQuery, BindRequired] bool onTheRecord)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var goOnTheRecordResult = await _depositionService.GoOnTheRecord(id, onTheRecord, userEmail);
            if (goOnTheRecordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(goOnTheRecordResult);

            return Ok(_depositionMapper.ToDto(goOnTheRecordResult.Value));
        }
    }
}
