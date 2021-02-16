using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Amazon.SimpleNotificationService.Util;
using System;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompositionsController : ControllerBase
    {
        private readonly ICompositionService _compositionService;
        private readonly ILogger<CompositionsController> _logger;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;

        public CompositionsController(ICompositionService compositionService,
            IMapper<Composition, CompositionDto, CallbackCompositionDto> compositionMapper,
            ILogger<CompositionsController> logger, IRoomService roomService)
        {
            _compositionService = compositionService;
            _compositionMapper = compositionMapper;
            _logger = logger;
            _roomService = roomService;
        }

        [ServiceFilter(typeof(ValidateTwilioRequestFilterAttribute))]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> CompositionStatusCallback([FromForm] CallbackCompositionDto compositionDto)
        {
            var compositionModel = _compositionMapper.ToModel(compositionDto);
            var updateCompositionResult = await _compositionService.UpdateCompositionCallback(compositionModel);
            if (updateCompositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(updateCompositionResult);

            return Ok();
        }

        [ServiceFilter(typeof(ValidateTwilioRequestFilterAttribute))]
        [HttpPost]
        [Route("recordings/addEvent")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> RoomStatusCallback([FromForm] RoomCallbackDto roomEvent)
        {
            var updateRoomStatusResult = await _roomService.UpdateStatusCallback(roomEvent.RoomSid, roomEvent.Timestamp, roomEvent.StatusCallbackEvent);
            if (updateRoomStatusResult.IsFailed)
                return WebApiResponses.GetErrorResponse(updateRoomStatusResult);

            return Ok();
        }

        [HttpPost]
        [Route("notify")]
        [Consumes("text/plain; charset=UTF-8")]
        public async Task<IActionResult> CompositionEditionCallback()
        {
            string content;
            using (var reader = new StreamReader(Request.Body)) { content = await reader.ReadToEndAsync(); }

            var message = Message.ParseMessage(content);

            if (!message.IsMessageSignatureValid())
                return BadRequest();

            if (message.IsSubscriptionType)
                await _compositionService.SubscribeEndpoint(message.SubscribeURL);

            if (message.IsNotificationType)
            {
                try
                {
                    var messageDto = (PostDepositionEditionDto)JsonConvert.DeserializeObject(message.MessageText, typeof(PostDepositionEditionDto));
                    await _compositionService.PostDepoCompositionCallback(messageDto);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    return BadRequest(e.Message);
                }
            }

            return Ok();
        }
    }
}
