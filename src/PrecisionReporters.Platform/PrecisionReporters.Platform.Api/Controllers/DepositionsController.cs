﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
using System.Net;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepositionsController : ControllerBase
    {
        private readonly IDepositionService _depositionService;
        private readonly IDocumentService _documentService;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;
        private readonly IMapper<BreakRoom, BreakRoomDto, object> _breakRoomMapper;
        private readonly IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto> _annotationMapper;
        private readonly IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto> _eventMapper;
        private readonly IAnnotationEventService _annotationEventService;
        private readonly IParticipantService _participantService;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Participant, ParticipantTechStatusDto, object> _participantTechStatusMapper;
        private readonly IMapper<Participant, AddParticipantDto, CreateGuestDto> _guestMapper;
        private readonly IMapper<UserSystemInfo, UserSystemInfoDto, object> _userSystemInfoMapper;
        private readonly IMapper<DeviceInfo, DeviceInfoDto, object> _userDeviceMapper;
        private readonly IMapper<Participant, SignInUnverifiedUserDto, object> _unverifiedParticipantMapper;

        public DepositionsController(IDepositionService depositionService,
            IMapper<Deposition, DepositionDto, CreateDepositionDto> depositionMapper,
            IDocumentService documentService, IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto> annotationMapper,
            IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto> eventMapper,
            IMapper<BreakRoom, BreakRoomDto, object> breakRoomMapper, IAnnotationEventService annotationEventService, IParticipantService partcipantService,
            IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper, IMapper<Participant, AddParticipantDto, CreateGuestDto> guestMapper,
            IMapper<Participant, ParticipantTechStatusDto, object> participantTechStatusMapper,
            IMapper<UserSystemInfo, UserSystemInfoDto, object> userSystemInfoMapper,
            IMapper<DeviceInfo, DeviceInfoDto, object> userDeviceMapper,
            IMapper<Participant, SignInUnverifiedUserDto, object> unverifiedParticipantMapper)
        {
            _depositionService = depositionService;
            _depositionMapper = depositionMapper;
            _documentService = documentService;
            _breakRoomMapper = breakRoomMapper;
            _annotationMapper = annotationMapper;
            _eventMapper = eventMapper;
            _annotationEventService = annotationEventService;
            _participantService = partcipantService;
            _participantMapper = participantMapper;
            _guestMapper = guestMapper;
            _participantTechStatusMapper = participantTechStatusMapper;
            _userSystemInfoMapper = userSystemInfoMapper;
            _userDeviceMapper = userDeviceMapper;
            _unverifiedParticipantMapper = unverifiedParticipantMapper;
        }

        [HttpGet]
        public async Task<ActionResult<DepositionFilterResponseDto>> GetDepositions([FromQuery] DepositionFilterDto filter)
        {
            var depositionResponseResult = await _depositionService.GetDepositionsByFilter(filter);

            if (depositionResponseResult.IsFailed)
                return WebApiResponses.GetErrorResponse(depositionResponseResult);

            return Ok(depositionResponseResult.Value);
        }

        /// <summary>
        /// Join to an existing Deposition
        /// </summary>
        /// <param name="id">DepositionId to Join.</param>
        /// <returns>JoinDepositionDto object.</returns>
        [HttpPost("{id}/join")]
        public async Task<ActionResult<JoinDepositionDto>> JoinDeposition([ResourceId(ResourceType.Deposition)] Guid id)
        {
            // TODO: review authorization

            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var joinDepositionInfoResult = await _depositionService.JoinDeposition(id, identity);

            if (joinDepositionInfoResult.IsFailed)
                return WebApiResponses.GetErrorResponse(joinDepositionInfoResult);

            return Ok(joinDepositionInfoResult.Value);
        }

        /// <summary>
        /// Resume an existing Deposition
        /// </summary>
        /// <param name="id">DepositionId to Resume.</param>
        /// <returns>JoinDepositionDto object.</returns>
        [HttpGet("{id}/summary")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<DepositionStatusDto>> Summary([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var joinDepositionInfoResult = await _depositionService.Summary(id);

            if (joinDepositionInfoResult.IsFailed)
                return WebApiResponses.GetErrorResponse(joinDepositionInfoResult);

            return Ok(joinDepositionInfoResult.Value);
        }

        /// <summary>
        /// End an existing Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to End.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpPost("{id}/end")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.EndDeposition)]
        public async Task<ActionResult<DepositionDto>> EndDeposition([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var endDepositionResult = await _depositionService.EndDeposition(id);
            if (endDepositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(endDepositionResult);

            return Ok(_depositionMapper.ToDto(endDepositionResult.Value));
        }

        /// <summary>
        /// Get an existing Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to End.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpGet("{id}")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewDetails)]
        public async Task<ActionResult<DepositionDto>> GetDeposition([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var endDepositionResult = await _depositionService.GetDepositionById(id);

            if (endDepositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(endDepositionResult);
            
            return Ok(_depositionMapper.ToDto(endDepositionResult.Value));
        }

        /// <summary>
        /// Change On Record status to a Deposition
        /// </summary>
        /// <param name="depositionId">DepositionId to add the OnRecord / OffRecord event.</param>
        /// <param name="onTheRecord">Status OnTheRecord / OffTheRecord event.</param>
        /// <returns>DepositionDto object.</returns>
        [HttpPost("{id}/record")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Recording)]
        public async Task<ActionResult<DepositionEventDto>> DepositionRecord([ResourceId(ResourceType.Deposition)] Guid id, [FromQuery, BindRequired] bool onTheRecord)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var goOnTheRecordResult = await _depositionService.GoOnTheRecord(id, onTheRecord, userEmail);
            if (goOnTheRecordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(goOnTheRecordResult);

            return Ok(_eventMapper.ToDto(goOnTheRecordResult.Value));
        }

        /// <summary>
        /// Join to an existing Break Room
        /// </summary>
        /// <param name="id">DepositionId to Identified current Deposition.</param>
        /// <param name="breakRoomId">Break Room Id.</param>
        /// <returns>Room Token string.</returns>
        [HttpPost("{id}/breakrooms/{breakRoomId}/join")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<string>> JoinBreakRoom([ResourceId(ResourceType.Deposition)] Guid id, Guid breakRoomId)
        {
            var joinBreakRoomResult = await _depositionService.JoinBreakRoom(id, breakRoomId);
            if (joinBreakRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(joinBreakRoomResult);

            return Ok(joinBreakRoomResult.Value);
        }

        /// <summary>
        /// Leave a Break Room
        /// </summary>
        /// <param name="id">DepositionId to Identified current Deposition.</param>
        /// <param name="breakRoomId">Break Room Id.</param>
        [HttpPost("{id}/breakrooms/{breakRoomId}/leave")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<IActionResult> LeaveBreakRoom([ResourceId(ResourceType.Deposition)] Guid id, Guid breakRoomId)
        {
            var leaveBreakRoomResult = await _depositionService.LeaveBreakRoom(id, breakRoomId);
            if (leaveBreakRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(leaveBreakRoomResult);

            return Ok();
        }

        /// <summary>
        /// Lock a Break Room
        /// </summary>
        /// <param name="id">DepositionId to Identified current Deposition.</param>
        /// <param name="breakRoomId">Break Room Id.</param>
        [HttpPost("{id}/breakrooms/{breakRoomId}/lock")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<BreakRoomDto>> LockBreakRoom([ResourceId(ResourceType.Deposition)] Guid id, Guid breakRoomId, bool lockRoom)
        {
            var lockBreakRoomResult = await _depositionService.LockBreakRoom(id, breakRoomId, lockRoom);
            if (lockBreakRoomResult.IsFailed)
                return WebApiResponses.GetErrorResponse(lockBreakRoomResult);

            return Ok(_breakRoomMapper.ToDto(lockBreakRoomResult.Value));
        }

        /// <summary>
        /// Get Break Rooms list
        /// </summary>
        /// <param name="id">DepositionId to Identified current Deposition.</param>
        /// <returns>A list of Break Rooms in the deposition.</returns>
        [HttpGet("{id}/breakrooms")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<List<BreakRoomDto>>> GetDepositionBreakRooms([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var breakRoomsResult = await _depositionService.GetDepositionBreakRooms(id);
            if (breakRoomsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(breakRoomsResult);

            return Ok(breakRoomsResult.Value.Select(i => _breakRoomMapper.ToDto(i)));
        }

        /// <summary>
        /// Gets the events of a an existing Deposition.
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>List of DepositionEvent of an existing Deposition</returns>
        [HttpGet("{id}/events")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<List<DepositionEventDto>>> GetDepositionEvents([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var eventsResult = await _depositionService.GetDepositionEvents(id);
            if (eventsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(eventsResult);

            return Ok(eventsResult.Value.Select(d => _eventMapper.ToDto(d)));
        }

        /// <summary>
        /// Add an annotation to a specific document
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Newly created annotation</returns>
        [HttpPost("{id}/SharedDocument/annotate")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult> AddDocumentAnnotation([ResourceId(ResourceType.Deposition)] Guid id, CreateAnnotationEventDto annotation)
        {
            var addAnnotationResult = await _documentService.AddAnnotation(id, _annotationMapper.ToModel(annotation));
            if (addAnnotationResult.IsFailed)
                return WebApiResponses.GetErrorResponse(addAnnotationResult);
            return Ok();
        }

        /// <summary>
        /// Get annotations related to a specific document
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="startingAnnotationId">Last Annotation identifier</param>
        /// <returns>Document's list of annotations events</returns>
        [HttpGet("{id}/SharedDocument/annotations")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewSharedDocument)]
        public async Task<ActionResult<List<AnnotationEventDto>>> GetDocumentAnnotations([ResourceId(ResourceType.Deposition)] Guid id, Guid? startingAnnotationId)
        {
            var annotationsResult = await _annotationEventService.GetDocumentAnnotations(id, startingAnnotationId);
            if (annotationsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(annotationsResult);

            return Ok(annotationsResult.Value.Select(d => _annotationMapper.ToDto(d)));
        }

        /// <summary>
        /// Checks if the email address belongs to a registered user.
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="emailAddress">User email address</param>
        /// <returns>A Participant if exists</returns>
        [HttpGet("{id}/checkParticipant")]
        [AllowAnonymous]
        public async Task<ActionResult<ParticipantValidationDto>> CheckParticipant([ResourceId(ResourceType.Deposition)] Guid id, string emailAddress)
        {
            var participantResult = await _depositionService.CheckParticipant(id, emailAddress.ToLower());
            if (participantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(participantResult);

            var participantValidation = new ParticipantValidationDto
            {
                IsUser = participantResult.Value.Item2,
                IsVerify = participantResult.Value.Item3,
                Participant = participantResult.Value.Item1 != null ? _participantMapper.ToDto(participantResult.Value.Item1) : null
            };

            return Ok(participantValidation);
        }

        /// <summary>
        /// Checks if the email address belongs to a registered user.
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>A Participant if exists</returns>
        [HttpPost("{id}/addGuestParticipant")]
        [AllowAnonymous]
        public async Task<ActionResult<GuestToken>> JoinGuestParticipant([ResourceId(ResourceType.Deposition)] Guid id, CreateGuestDto guest)
        {
            var publicIPAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(new[] { ',' }).FirstOrDefault();
            var localIPAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4();
            //TODO: Add mapper
            var activity = new ActivityHistory
            {
                Browser = guest.Browser,
                Device = guest.Device,
                IPAddress = string.IsNullOrWhiteSpace(publicIPAddress) ? localIPAddress.ToString() : publicIPAddress
            };

            var tokenResult = await _depositionService.JoinGuestParticipant(id, _guestMapper.ToModel(guest), activity);
            if (tokenResult.IsFailed)
                return WebApiResponses.GetErrorResponse(tokenResult);

            return Ok(tokenResult.Value);
        }

        /// <summary>
        /// Add a registered participant to the deposition
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPost("{id}/addParticipant")]
        [AllowAnonymous]
        public async Task<ActionResult<Guid>> AddParticipant([ResourceId(ResourceType.Deposition)] Guid id, AddParticipantDto participant)
        {
            var addParticipantResult = await _depositionService.AddParticipant(id, _guestMapper.ToModel(participant));

            if (addParticipantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(addParticipantResult);

            return Ok(new ParticipantOutputDto() { Id = addParticipantResult.Value });
        }

        /// <summary>
        /// Gets the public url of a deposition video.
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <returns></returns>
        [HttpGet("{id}/video")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<DepositionVideoDto>> GetDepositionVideo([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var videoInformationResult = await _depositionService.GetDepositionVideoInformation(id);
            if (videoInformationResult.IsFailed)
                return WebApiResponses.GetErrorResponse(videoInformationResult);

            return Ok(videoInformationResult.Value);
        }

        /// <summary>
        /// Get Participant list by Deposition ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sortField"></param>
        /// <param name="sortDirection"></param>
        /// <returns>Ok if succeed</returns>
        [HttpGet("{id}/participants")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<List<ParticipantDto>>> GetParticipantList([ResourceId(ResourceType.Deposition)] Guid id,
            ParticipantSortField sortField = ParticipantSortField.Role,
            SortDirection sortDirection = SortDirection.Descend)
        {
            var lstParticipantResult = await _depositionService.GetDepositionParticipants(id, sortField, sortDirection);
            if (lstParticipantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(lstParticipantResult);
            return Ok(lstParticipantResult.Value.Select(p => _participantMapper.ToDto(p)).ToList());
        }

        /// <summary>
        /// Add a registered participant to the deposition
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="participant">Participant data</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPost("{id}/participants")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Update)]
        public async Task<ActionResult<ParticipantDto>> AddParticipantToExistingDeposition([ResourceId(ResourceType.Deposition)] Guid id, CreateParticipantDto participant)
        {
            var addParticipantResult = await _depositionService.AddParticipantToExistingDeposition(id,
                _participantMapper.ToModel(participant));

            if (addParticipantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(addParticipantResult);

            return Ok(_participantMapper.ToDto(addParticipantResult.Value));
        }

        /// <summary>
        /// Edit deposition details
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="Details">Participant identifier</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPatch("{id}")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Update)]
        public async Task<ActionResult<DepositionDto>> EditDepositionDetails([ResourceId(ResourceType.Deposition)] Guid id, EditDepositionDto editDepositionDto)
        {
            var file = Request.HasFormContentType ?
                FileHandlerHelper.GetFilesFromRequest(Request.Form.Files).FirstOrDefault().Value
                : null;
            editDepositionDto.Deposition.Id = id;
            var deposition = _depositionMapper.ToModel(editDepositionDto.Deposition);
            var result = await _depositionService.EditDepositionDetails(deposition, file, editDepositionDto.DeleteCaption);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(_depositionMapper.ToDto(result.Value));
        }

        [HttpPost("{id}/joinResponse/{participantId}")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.AdmitParticipants)]
        public async Task<ActionResult> AdmitParticipant([ResourceId(ResourceType.Deposition)] Guid id, Guid participantId, [FromBody] JoinDepositionResponseDto admitParticipant)
        {
            var result = await _depositionService.AdmitDenyParticipant(participantId, admitParticipant.IsAdmitted);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result);
        }

        [HttpGet("{id}/waitingRoomParticipants")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.AdmitParticipants)]
        public async Task<ActionResult<List<ParticipantDto>>> GetWaitParticipants([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var participantResult = await _participantService.GetWaitParticipants(id);
            if (participantResult.Value == null)
                return new List<ParticipantDto>();

            return Ok(participantResult.Value.Select(p => _participantMapper.ToDto(p)).ToList());
        }

        [HttpPost("{id}/cancel")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Cancel)]
        public async Task<ActionResult> CancelDeposition([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var cancelDepositionResult = await _depositionService.CancelDeposition(id);
            if (cancelDepositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(cancelDepositionResult);

            return Ok(_depositionMapper.ToDto(cancelDepositionResult.Value));
        }

        /// <summary>
        /// Revert deposition cancel
        /// </summary>
        /// <param name="id">Deposition identifier</param>
        /// <param name="editDepositionDto">Deposition data</param>
        /// <returns>Ok if succeeded</returns>
        [HttpPost("{id}/revertCancel")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Revert)]
        public async Task<ActionResult<DepositionDto>> RevertCancel([ResourceId(ResourceType.Deposition)] Guid id, EditDepositionDto editDepositionDto)
        {
            var file = Request.HasFormContentType ?
                FileHandlerHelper.GetFilesFromRequest(Request.Form.Files).FirstOrDefault().Value
                : null;
            editDepositionDto.Deposition.Id = id;
            var deposition = _depositionMapper.ToModel(editDepositionDto.Deposition);
            var result = await _depositionService.RevertCancel(deposition, file, editDepositionDto.DeleteCaption);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(_depositionMapper.ToDto(result.Value));
        }

        [HttpPost("{id}/reschedule")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ReSchedule)]
        public async Task<ActionResult<DepositionDto>> ReScheduleDeposition([ResourceId(ResourceType.Deposition)] Guid id, EditDepositionDto editDepositionDto)
        {
            var file = Request.HasFormContentType ?
                FileHandlerHelper.GetFilesFromRequest(Request.Form.Files).FirstOrDefault().Value
                : null;
            editDepositionDto.Deposition.Id = id;
            var deposition = _depositionMapper.ToModel(editDepositionDto.Deposition);
            var result = await _depositionService.ReScheduleDeposition(deposition, file, editDepositionDto.DeleteCaption);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(_depositionMapper.ToDto(result.Value));
        }

        [HttpPost("{id}/notifyparties")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.Notify)]
        public async Task<ActionResult<NotifyOutputDto>> NotifyParties([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var result = await _depositionService.NotifyParties(id);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(new NotifyOutputDto { Notified = result.Value });
        }

        /// <summary>
        /// Get an existing Deposition info for tech status tab
        /// </summary>
        /// <param name="depositionId">DepositionId to End.</param>
        /// <returns>DepositionTechStatusDto object.</returns>
        [HttpGet("{id}/info")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.ViewDepositionStatus)]
        public async Task<ActionResult<DepositionTechStatusDto>> GetDepositionInfo([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var depositionResult = await _depositionService.GetByIdWithIncludes(id, new[] { nameof(Deposition.SharingDocument), nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.DeviceInfo)}",
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}.{nameof(Data.Entities.User.ActivityHistories)}"});
            if (depositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(depositionResult);

            return Ok(new DepositionTechStatusDto
            {
                RoomId = depositionResult.Value.Room.SId?.ToString(),
                IsVideoRecordingNeeded = depositionResult.Value.IsVideoRecordingNeeded,
                IsRecording = depositionResult.Value.IsOnTheRecord,
                SharingExhibit = depositionResult?.Value?.SharingDocument?.DisplayName,
                Participants = depositionResult.Value.Participants.Where(t => t.HasJoined && t.User != null)
                .Select(p => _participantTechStatusMapper.ToDto(p)).ToList()
            });
        }

        [HttpPost("{id}/userSystemInfo")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult> SetUserSystemInfo([ResourceId(ResourceType.Deposition)] Guid id, UserSystemInfoDto userSystemInfoDto)
        {
            // TODO: review authorization
            var publicIPAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(new[] { ',' }).FirstOrDefault();
            var localIPAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4();

            string ipAddress = string.IsNullOrWhiteSpace(publicIPAddress) ? localIPAddress.ToString() : publicIPAddress;

            var userSystemInfo = _userSystemInfoMapper.ToModel(userSystemInfoDto);

            var result = await _depositionService.UpdateUserSystemInfo(id, userSystemInfo, ipAddress);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result);
        }

        [HttpPost("{id}/devices")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult> SetUserDevice([ResourceId(ResourceType.Deposition)] Guid id, DeviceInfoDto userDeviceDto)
        {
            // TODO: review authorization
            var userDeviceInfo = _userDeviceMapper.ToModel(userDeviceDto);

            var result = await _participantService.SetUserDeviceInfo(id, userDeviceInfo);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result);
        }

        [HttpPost("{id}/awsInfo")]
        public async Task<ActionResult> SaveAWSInfo(Guid id)
        {
            var result = await _depositionService.SaveAwsSessionInfo(id, Dns.GetHostName());
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(result);
        }

        /// <summary>
        /// Login an Unverified user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unverifiedUser"></param>
        /// <returns></returns>
        [HttpPost("{id}/addUnVerifyParticipant")]
        [AllowAnonymous]
        public async Task<ActionResult<GuestToken>> JoinUnverifiedParticipant([ResourceId(ResourceType.Deposition)] Guid id, SignInUnverifiedUserDto unverifiedUser)
        {
            var publicIPAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(new[] { ',' }).FirstOrDefault();
            var localIPAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4();
            //TODO: Add mapper
            var activity = new ActivityHistory
            {
                Browser = unverifiedUser.Browser,
                Device = unverifiedUser.Device,
                IPAddress = string.IsNullOrWhiteSpace(publicIPAddress) ? localIPAddress.ToString() : publicIPAddress
            };

            var tokenResult = await _depositionService.JoinUnverifiedParticipant(id, _unverifiedParticipantMapper.ToModel(unverifiedUser), activity);
            if (tokenResult.IsFailed)
                return WebApiResponses.GetErrorResponse(tokenResult);

            return Ok(tokenResult.Value);
        }
    }
}
