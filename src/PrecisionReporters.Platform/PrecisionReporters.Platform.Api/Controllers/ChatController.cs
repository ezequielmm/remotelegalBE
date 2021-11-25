using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("{id}/token")]
        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<ActionResult<JoinChatDto>> GetChatTokenById([ResourceId(ResourceType.Deposition)] Guid id)
        {
            var identity = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            var joinChatResult = await _chatService.ManageChatParticipant(id);

            if (joinChatResult.IsFailed)
                return WebApiResponses.GetErrorResponse(joinChatResult);

            return Ok(joinChatResult.Value);
        }
    }
}
