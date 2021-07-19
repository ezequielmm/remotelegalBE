using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Authorization.Attributes;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Mappers;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ParticipantsController : ControllerBase
    {
        private readonly IParticipantService _participantService;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Participant, EditParticipantDto, object> _editParticipantMapper;

        public ParticipantsController(IParticipantService participantService,
            IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper,
            IMapper<Participant, EditParticipantDto, object> editParticipantMapper)
        {
            _participantService = participantService;
            _participantMapper = participantMapper;
            _editParticipantMapper = editParticipantMapper;
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
            
            return Ok(participantResult.Value);
        }

        /// <summary>
        /// Update, send Participan Status and notify join
        /// </summary>
        /// <param name="id">Identifier of the deposition which the participant belongs to.</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPut]
        [Route("Depositions/{id}/notifyParticipantPresence")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<ParticipantStatusDto>> NotifyParticipantPresence([ResourceId(ResourceType.Deposition)] Guid id, ParticipantStatusDto participantStatus)
        {
            var participantResult = await _participantService.NotifyParticipantPresence(participantStatus, id);

            if (participantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(participantResult);

            return Ok(participantResult.Value);
        }

        /// <summary>
        /// Remove a registered participant from the deposition
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="participantId">Participant identifier</param>
        /// <returns>Ok if succeeded</returns>
        [HttpDelete("Depositions/{id}/participants/{participantId}")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Update)]
        public async Task<ActionResult<Guid>> RemoveParticipantFromExistingDeposition([ResourceId(ResourceType.Deposition)] Guid id, Guid participantId)
        {
            var result = await _participantService.RemoveParticipantFromDeposition(id, participantId);

            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result);
        }

        /// <summary>
        /// Edit a registered participant details
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="participant">Edited participant details</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPatch("Depositions/{id}/editParticipant")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Update)]
        public async Task<ActionResult<ParticipantDto>> EditParticipant([ResourceId(ResourceType.Deposition)] Guid id, EditParticipantDto participant)
        {
            var participantToEdit = _editParticipantMapper.ToModel(participant);
            var editParticipantResult = await _participantService.EditParticipantDetails(id, participantToEdit);

            if (editParticipantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(editParticipantResult);

            return Ok(_participantMapper.ToDto(editParticipantResult.Value));
        }
    }
}
