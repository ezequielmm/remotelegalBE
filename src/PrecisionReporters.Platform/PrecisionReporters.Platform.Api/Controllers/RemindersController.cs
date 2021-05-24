using Amazon.SimpleNotificationService.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
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

        public RemindersController(
            ILogger<RemindersController> logger,
            IReminderService reminderService)
        {
            _logger = logger;
            _reminderService = reminderService;
        }

        [HttpPost]
        [Route("Reminder")]
        [Consumes("text/plain; charset=UTF-8")]
        public async Task<IActionResult> Reminder()
        {
            _logger.LogInformation($"Init reminder method");
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

            try
            {
                await _reminderService.SendReminder();
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
            _logger.LogInformation($"Init daily reminder method");
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
