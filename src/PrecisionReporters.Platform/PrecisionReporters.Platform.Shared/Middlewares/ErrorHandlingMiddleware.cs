﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly bool _showMessage;

        public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger, RequestDelegate next,
            bool showMessage)
        {
            _logger = logger;
            _next = next;
            _showMessage = showMessage;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BaseException ex)
            {
                context.Response.StatusCode = ex.StatusCode;
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                if (context.WebSockets != null && context.WebSockets.IsWebSocketRequest)
                {
                    _logger.LogError(ex, ex.Message);
                }
                else 
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await HandleExceptionAsync(context, ex);
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            
            var message = (exception is BaseException) ? exception.Message : "Internal server error";
            var error = (_showMessage) ? exception : null;
            var result = JsonConvert.SerializeObject(new { Message = message, Error = error });
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsync(result);
        }

    }
}