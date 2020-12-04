using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
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

        public DepositionsController(IDepositionService depositionService)
        {
            _depositionService = depositionService;
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