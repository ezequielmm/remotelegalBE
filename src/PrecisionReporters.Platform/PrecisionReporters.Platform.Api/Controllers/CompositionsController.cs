﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Api.Filters;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;

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
        private readonly ISnsHelper _snsHelper;
        private readonly IAwsSnsWrapper _awsSnsWrapper;

        public CompositionsController(ICompositionService compositionService,
            IMapper<Composition, CompositionDto, CallbackCompositionDto> compositionMapper,
            ILogger<CompositionsController> logger,
            ITwilioCallbackService twilioCallbackService, 
            ISnsHelper snsHelper,
            IAwsSnsWrapper awsSnsWrapper)
        {
            _compositionService = compositionService;
            _compositionMapper = compositionMapper;
            _logger = logger;
            _twilioCallbackService = twilioCallbackService;
            _snsHelper = snsHelper;
            _awsSnsWrapper = awsSnsWrapper;
        }

        [ServiceFilter(typeof(ValidateTwilioRequestFilterAttribute))]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> CompositionStatusCallback([FromForm] CallbackCompositionDto compositionDto)
        {
            var compositionModel = _compositionMapper.ToModel(compositionDto);
            var updateCompositionResult = await _compositionService.UpdateCompositionCallback(compositionModel);
            if (updateCompositionResult.IsFailed)
            {
                _logger.LogError("There was an error updating the composition with SId: {0} from the room SId: {1}", compositionDto.CompositionSid, compositionDto.RoomSid);
                return WebApiResponses.GetErrorResponse(updateCompositionResult);
            }

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

            var message = _awsSnsWrapper.ParseMessage(content);
            // TODO: We should process these Sns Message using /api/Notications/SnsCallback (we just need to create a Handler to process message type: PostDepositionEditionDto)
            // avoid subscribe and validate message on Service layer
            if (!_awsSnsWrapper.IsMessageSignatureValid(message))
            {
                _logger.LogError("There was an error verifying the authenticity of a message sent by Amazon SNS.");
                return BadRequest();
            }

            if (message.IsSubscriptionType)
            {
                var result = await _snsHelper.SubscribeEndpoint(message.SubscribeURL);
                if (result.IsFailed)
                    _logger.LogError("Error on validate and confirm the endpoint added as a valid destination to receive messages from aws notification service, {0}", result);
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
