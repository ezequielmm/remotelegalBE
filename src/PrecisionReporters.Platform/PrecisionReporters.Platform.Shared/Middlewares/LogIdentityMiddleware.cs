using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Middlewares
{
    public class LogIdentityMiddleware
    {
        private readonly RequestDelegate _next;

        public LogIdentityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            using (LogContext.PushProperty("identity", new
            {
                authenticationType = context.User?.Identity?.AuthenticationType,
                isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
                nameidentifier = context.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"))?.Value,
                aud = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("aud"))?.Value,
                email_verified = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("email_verified"))?.Value,
                token_use = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("token_use"))?.Value,
                auth_time = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("auth_time"))?.Value,
                iss = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("iss"))?.Value,
                username = context.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("username"))?.Value,
                exp = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("exp"))?.Value,
                iat = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("iat"))?.Value,
                emailaddress = context.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("emailaddress"))?.Value,
                event_id = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("event_id"))?.Value,
                groups = context.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("groups"))?.Value
            }, true))
            {
                return _next(context);
            }
        }
    }
}
