using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Authorization.Attributes;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]

    public class ParticipantsController : ControllerBase
    {
        public readonly IParticipantService _participantService;

        public ParticipantsController(IParticipantService participantService)
        {
            _participantService = participantService;
        }

        /// <summary>
        /// Update and send Participan Status
        /// </summary>
        /// <param name="id">Identifier of the deposition which the participant belongs to.</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPut]
        [Route("Depositions/{id}/participantStatus")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<ParticipantStatusDto>> ParticipantStatus([ResourceId(ResourceType.Deposition)] Guid id, ParticipantStatusDto participantStatus)
        {   
            var participantResult = await _participantService.UpdateParticipantStatus(participantStatus, id);
            
            if (participantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(participantResult);
            
            return Ok(participantResult);
        }
    }
}
