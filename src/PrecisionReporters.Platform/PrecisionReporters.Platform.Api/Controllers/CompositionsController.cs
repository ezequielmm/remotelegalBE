using Amazon.SimpleNotificationService.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompositionsController : ControllerBase
    {
        private readonly ICompositionService _compositionService;
        private readonly ILogger<CompositionsController> _logger;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;
        private readonly ITwilioCallbackService _twilioCallbackService;

        public CompositionsController(ICompositionService compositionService,
            IMapper<Composition, CompositionDto, CallbackCompositionDto> compositionMapper,
            ILogger<CompositionsController> logger, 
            ITwilioCallbackService twilioCallbackService)
        {
            _compositionService = compositionService;
            _compositionMapper = compositionMapper;
            _logger = logger;
            _twilioCallbackService = twilioCallbackService;
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
            var updateRoomStatusResult = await _twilioCallbackService.UpdateStatusCallback(roomEvent);
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
            {
                var result = await SnsHelper.SubscribeEndpoint(message.SubscribeURL);
                if (result.IsFailed)
                    _logger.LogError($"There was an error subscribing URL, {result}");
            }

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
