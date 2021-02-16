using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IDepositionService _depositionService;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;

        public PermissionsController(IDepositionService depositionService, IPermissionService permissionService, IUserService userService)
        {
            _depositionService = depositionService;
            _permissionService = permissionService;
            _userService = userService;
        }

        [HttpGet("depositions/{id}")]
        public async Task<ActionResult<DepositionPermissionsDto>> GetDepositionPermissionsForParticipant(Guid id)
        {
            var userEmail = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var userResult = await _userService.GetUserByEmail(userEmail);

            if (!userResult.IsFailed && userResult.Value.IsAdmin)
            {
                var adminPermissions = await _permissionService.GetDepositionUserPermissions(null, Guid.Empty, true);
                var adminPermissionsDto = new DepositionPermissionsDto
                {
                    Role = ParticipantType.Admin,
                    IsAdmin = true,
                    Permissions = adminPermissions.Value.ToList()
                };
                return Ok(adminPermissionsDto);
            }

            var participantResult = await _depositionService.GetDepositionParticipantByEmail(id, userEmail);

            if (participantResult.IsFailed)
                return WebApiResponses.GetErrorResponse(participantResult);

            var participant = participantResult.Value;
            var permissionsResult = await _permissionService.GetDepositionUserPermissions(participant, id);

            if (permissionsResult.IsFailed)
                return WebApiResponses.GetErrorResponse(permissionsResult);

            var depositionPermissionsDto = new DepositionPermissionsDto
            {
                Role = participant.Role,
                IsAdmin = participant.User != null && participant.User.IsAdmin,
                Permissions = permissionsResult.Value.ToList()
            };
            return Ok(depositionPermissionsDto);
        }
    }
}
