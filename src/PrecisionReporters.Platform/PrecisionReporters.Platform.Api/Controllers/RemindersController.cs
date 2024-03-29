﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/depositions")]
    [ApiController]
    public class RemindersController : ControllerBase
    {
        private readonly ILogger<RemindersController> _logger;
        private readonly IReminderService _reminderService;
        private readonly IAwsSnsWrapper _awsSnsWrapper;
        private readonly ISnsHelper _snsHelper;

        public RemindersController(
            ILogger<RemindersController> logger,
            IReminderService reminderService,
            IAwsSnsWrapper awsSnsWrapper,
            ISnsHelper snsHelper)
        {
            _logger = logger;
            _reminderService = reminderService;
            _awsSnsWrapper = awsSnsWrapper;
            _snsHelper = snsHelper;
        }

        [HttpPost]
        [Route("Reminder")]
        [Consumes("text/plain; charset=UTF-8")]
        public async Task<IActionResult> Reminder()
        {
            // TODO: We should process these Sns Message using /api/Notications/SnsCallback (we just need to create a Handler to process message type: Reminder)
            // avoid subscribe and validate message on Service layer
            _logger.LogInformation($"Init reminder method");
            string content;
            using (var reader = new StreamReader(Request.Body)) { content = await reader.ReadToEndAsync().ConfigureAwait(false); }

            var message = _awsSnsWrapper.ParseMessage(content);

            if (!_awsSnsWrapper.IsMessageSignatureValid(message))
                return BadRequest();

            if (message.IsSubscriptionType)
            {
                var result = await _snsHelper.SubscribeEndpoint(message.SubscribeURL).ConfigureAwait(false);
                if (result.IsFailed)
                    _logger.LogError($"There was an error subscribing URL, {result}");
            }

            try
            {
                await _reminderService.SendReminder().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }
            return Ok();
        }

        [HttpPost]
        [Route("DailyReminder")]
        [Consumes("text/plain; charset=UTF-8")]
        public async Task<IActionResult> DayBeforeReminder()
        {
            // TODO: We should process these Sns Message using /api/Notications/SnsCallback (we just need to create a Handler to process message type: DayBeforeReminder)
            // avoid subscribe and validate message on Service layer
            _logger.LogInformation($"Init daily reminder method");
            string content;
            using (var reader = new StreamReader(Request.Body)) { content = await reader.ReadToEndAsync().ConfigureAwait(false); }

            var message = _awsSnsWrapper.ParseMessage(content);

            if (!_awsSnsWrapper.IsMessageSignatureValid(message))
                return BadRequest();

            if (message.IsSubscriptionType)
            {
                var result = await _snsHelper.SubscribeEndpoint(message.SubscribeURL).ConfigureAwait(false);
                if (result.IsFailed)
                    _logger.LogError($"There was an error subscribing URL, {result}");
            }

            try
            {
                await _reminderService.SendDailyReminder();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }
            return Ok();
        }
    }
}
