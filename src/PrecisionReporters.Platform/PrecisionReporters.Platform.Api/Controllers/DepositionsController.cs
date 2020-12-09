using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
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

        public DepositionsController(IDepositionService depositionService,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper)
        {
            _depositionService = depositionService;
            _depositionMapper = depositionMapper;
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
    }
}
