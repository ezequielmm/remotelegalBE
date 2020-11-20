using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Configurations;
using Twilio.Security;

namespace PrecisionReporters.Platform.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateTwilioRequestFilterAttribute : ActionFilterAttribute
    {
        private readonly RequestValidator _requestValidator;
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;
        private readonly ILogger<ValidateTwilioRequestFilterAttribute> _log;

        public ValidateTwilioRequestFilterAttribute(IOptions<TwilioAccountConfiguration> twilioAccountConfiguration,
            ILogger<ValidateTwilioRequestFilterAttribute> log)
        {
            _twilioAccountConfiguration = twilioAccountConfiguration.Value ?? throw new ArgumentException(nameof(twilioAccountConfiguration));
            _requestValidator = new RequestValidator(_twilioAccountConfiguration.AuthToken);
            _log = log;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var context = actionContext.HttpContext;
            if (!IsValidRequest(context.Request))
            {
                actionContext.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                actionContext.Result = new BadRequestObjectResult(actionContext.ModelState);
            }

            base.OnActionExecuting(actionContext);
        }

        private bool IsValidRequest(HttpRequest request)
        {
            var requestUrl = RequestRawUrl(request);
            var parameters = request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            var signature = request.Headers["X-Twilio-Signature"];

            _log.LogDebug("Validating Twilio callback");
            _log.LogDebug($"requestUrl: {requestUrl}");
            _log.LogDebug($"signature: {signature}");
            _log.LogDebug($"parameters: {string.Join(",", parameters.Select(kv => kv.Key + "=" + kv.Value).ToArray())}");
            _log.LogDebug($"Headers: {string.Join(", ", request.Headers.Select(kv => kv.Key + " = " + kv.Value).ToArray())}");

            var result = _requestValidator.Validate(requestUrl, parameters, signature);

            _log.LogDebug($"Request validation result: {result}");

            return result;
        }

        private string RequestRawUrl(HttpRequest request)
        {
            // We could use methods from HttpRequest to build the final URL, but
            // Twilio requires us to use this format, so we use this one to be closer
            // to their documentation requirements.
            return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        }
    }
}
